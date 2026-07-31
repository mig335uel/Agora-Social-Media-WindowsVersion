using System;
using System.Linq;
using System.Threading.Tasks;
using Agora.Models;
using Agora.Services.interfaces;
using Agora.Security;
using Windows.Storage;

namespace Agora.Services
{
    public class AuthService : IAuthentication
    {
        // 1. Instancia estática (Singleton) para llamarlo cómodamente sin hacer "new" cada vez
        private static AuthService? _instance;
        public static AuthService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AuthService();
                }
                return _instance;
            }
        }

        // Constructor privado para obligar a usar AuthService.Instance
        private AuthService() { }

        // =======================================================
        // FASE 1: REGISTRO DEL DISPOSITIVO (E2EE)
        // =======================================================
        private async Task RegistrarHardwareEnSupabase(string userId)
        {
            try
            {
                // 1. Generar/Obtener la llave pública del chip seguro TPM 2.0
                string publicKey = AgoraBunker.GenerarYObtenerClavePublica();

                // 2. Obtener un identificador único del PC que persista
                var localSettings = ApplicationData.Current.LocalSettings;
                string deviceId;
                if (localSettings.Values.ContainsKey("AgoraDeviceId"))
                {
                    deviceId = (string)localSettings.Values["AgoraDeviceId"];
                }
                else
                {
                    deviceId = "device_win_" + Guid.NewGuid().ToString("N");
                    localSettings.Values["AgoraDeviceId"] = deviceId;
                }

                // 3. Comprobar si ya existe un dispositivo con este hardware ID
                var response = await SupabaseConnect.Client!.From<Devices>()
                    .Filter("device_identifier", Supabase.Postgrest.Constants.Operator.Equals, deviceId)
                    .Get();

                var existingDevice = response.Models.FirstOrDefault();

                if (existingDevice != null)
                {
                    // Actualizar el existente (el ID se mantiene)
                    existingDevice.FcmToken = "windows_push_pending";
                    existingDevice.PublicDeviceKey = publicKey;
                    existingDevice.DeviceName = Environment.MachineName;
                    existingDevice.LastSeen = DateTime.Now;
                    await SupabaseConnect.Client!.From<Devices>().Update(existingDevice);
                }
                else
                {
                    // Insertar uno nuevo (Supabase generará el ID automáticamente)
                    var device = new Devices
                    {
                        DeviceIdentifier = deviceId,
                        UserId = userId,
                        FcmToken = "windows_push_pending", // Preparado para WNS en el futuro
                        PublicDeviceKey = publicKey,
                        DeviceName = Environment.MachineName,
                        Platform = "windows",
                        CreatedAt = DateTime.Now,
                        LastSeen = DateTime.Now
                    };
                    await SupabaseConnect.Client!.From<Devices>().Insert(device);
                }
                System.Diagnostics.Debug.WriteLine("[Búnker] Hardware de Windows blindado y registrado con éxito.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Búnker] Error al registrar hardware: {ex.Message}");
            }
        }

        public async Task RestoreSessionManuallyAsync()
        {
            try
            {
                string? json = AgoraBunker.ExtraerPayloadSync("agora_session_bunker.dat");
                if (!string.IsNullOrEmpty(json))
                {
                    var session = Newtonsoft.Json.JsonConvert.DeserializeObject<Supabase.Gotrue.Session>(json);
                    if (session != null && !string.IsNullOrEmpty(session.AccessToken))
                    {
                        await SupabaseConnect.Client!.Auth.SetSession(session.AccessToken, session.RefreshToken);
                        System.Diagnostics.Debug.WriteLine("[Búnker] Sesión restaurada a la fuerza con éxito.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Búnker] Error al cargar sesión manualmente: {ex.Message}");
            }
        }

        public async Task<bool> SignInAsync(LoginForm login)
        {
            try
            {
                var session = await SupabaseConnect.Client!.Auth.SignIn(login.Email, login.Password);
                
                if (session != null && session.User != null)
                {
                    // Forzar guardado de la sesión en disco (búnker TPM)
                    var persister = new CustomSessionPersistence();
                    persister.SaveSession(session);

                    // Al iniciar sesión en un PC, blindamos su hardware en Supabase para el E2EE
                    await RegistrarHardwareEnSupabase(session.User.Id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SignIn: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SignUpAsync(RegisterForm form)
        {
            try
            {
                // A) Crear el usuario en Supabase Auth

                var session = await SupabaseConnect.Client!.Auth.SignUp(form.Email, form.Password);
                
                if (session == null || session.User == null)
                    return false;

                // B) Guardar los datos extra en nuestra base de datos (tabla users)
                var dbUser = new Users(
                    id: session.User.Id,
                    name: form.Name,
                    lastName: form.LastName,
                    username: form.Username,
                    displayName: form.DisplayName,
                    bio: "",
                    profilePictureUrl: "",
                    birthDate: form.BirthDate,
                    gender: form.Gender,
                    createdAt: DateTime.Now,
                    advertencias: 0,
                    penalizaciones: 0
                );
                

                await SupabaseConnect.Client.From<Users>().Insert(dbUser);
                
                // C) Registrar el hardware al terminar el registro
                await RegistrarHardwareEnSupabase(session.User.Id);

                // D) Forzar guardado de la sesión en disco (búnker TPM)
                var persister = new CustomSessionPersistence();
                persister.SaveSession(session);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SignUp: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SignOutAsync()
        {
            try
            {
                // 1. Eliminar completamente el dispositivo de la base de datos al cerrar sesión
                if (SupabaseConnect.Client!.Auth.CurrentSession != null)
                {
                    var deviceId = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AgoraDeviceId"] as string;
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        var response = await SupabaseConnect.Client.From<Devices>()
                            .Filter("device_identifier", Supabase.Postgrest.Constants.Operator.Equals, deviceId)
                            .Get();
                        var existingDevice = response.Models.FirstOrDefault();
                        if (existingDevice != null)
                        {
                            await SupabaseConnect.Client.From<Devices>().Delete(existingDevice);
                        }
                    }
                }

                // 2. Cerrar sesión en Supabase (Invalida el token en el servidor de GoTrue)
                await SupabaseConnect.Client!.Auth.SignOut();

                // 3. Destruir físicamente el archivo encriptado de la sesión del Búnker
                var persister = new CustomSessionPersistence();
                persister.DestroySession();

                System.Diagnostics.Debug.WriteLine("[Búnker] Sesión de usuario destruida con éxito.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SignOut: {ex.Message}");
                return false;
            }
        }
    }
}
