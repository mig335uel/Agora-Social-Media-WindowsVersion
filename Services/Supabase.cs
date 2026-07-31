using System;
using System.Threading.Tasks;
using Agora.Security;
using Supabase;
using Supabase.Gotrue.Interfaces;
using System.IO;
using System.Text.Json;

namespace Agora.Services
{
    public class SupabaseConnect
    {
        // Singleton para acceder a la BD desde cualquier ViewModel o Vista
        public static Supabase.Client? Client { get; private set; }

        public static async Task InitializeAsync()
        {
            // 1. Despertar al Búnker (Verifica C++ y descarga la llave si es la primera vez)
            await AgoraBunker.InicializarBunkerAsync();

            // 2. Extraer la llave real del hardware TPM 2.0
            string? realApiKey = await AgoraBunker.ExtraerClaveDelBunkerAsync();

            if (string.IsNullOrEmpty(realApiKey))
            {
                // Si llegamos aquí sin llave, abortamos por seguridad
                Environment.Exit(unchecked((int)0xDEAD));
            }

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
                SessionHandler = new CustomSessionPersistence()
            };

            // 3. Inicializar Supabase con la llave REAL extraída del hardware
            // Para que Realtime funcione sin problemas, metemos la llave directamente.
            // La seguridad activa la mantendrá el TpmBunkerInterceptor evaluando la RAM.
            Client = new Supabase.Client("https://zqcgontfrcuofeqrtvvq.supabase.co", realApiKey, options);

            // 4. INYECCIÓN DEL ESCUDO TPM
            // Para no romper las sub-URLs (ej. /auth/v1/ o /rest/v1/), leemos el cliente original,
            // copiamos sus cabeceras y URL, y le añadimos nuestro interceptor.
            InjectHttpClient(Client.Auth);
            InjectHttpClient(Client.Postgrest);
            InjectHttpClient(Client.Storage);
            InjectHttpClient(Client.Functions);

            // 5. INYECCIÓN DEL SSL PINNING EN WEBSOCKETS (ANTES DE CONECTAR)
            // Ya que AutoConnectRealtime es true, se conectará en InitializeAsync().
            // Usamos Reflection para inyectar la factoría sin importar la versión de Websocket.Client.
            if (Client.Realtime.Socket is Websocket.Client.WebsocketClient wsClient)
            {
                Func<System.Net.WebSockets.ClientWebSocket> newFactory = () =>
                {
                    if(AgoraBunker.AgoraIsEnvironmentCompromised())
                    {
                        System.Diagnostics.Debug.WriteLine("[Búnker] Entorno comprometido, abortando conexión WebSocket.");
                        Environment.Exit(unchecked((int)0xDEAD));
                    }
                    var client = new System.Net.WebSockets.ClientWebSocket();
                    client.Options.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                    {
                        if (cert == null) return false;
                        byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(cert.GetPublicKey());
                        string serverPin = Convert.ToBase64String(hashBytes);
                        return serverPin == "VUUMLi+n7iaAw+X9sfcsrHMs8MqLG+WewadiiymYWEA=" || 
                               serverPin == "PD6PUu2Rh7NKN+wAgWFnGn5Hw+IELlAOn9so3A32QYE=";
                    };
                    return client;
                };

                var field = wsClient.GetType().GetField("_clientFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ??
                            wsClient.GetType().GetField("<ClientFactory>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ??
                            wsClient.GetType().GetField("clientFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(wsClient, newFactory);
                }
            }

            // 6. Enganchar evento para asegurar que si el token se renueva en segundo plano, se guarde en disco
            Client.Auth.AddStateChangedListener((sender, state) =>
            {
                if (state == Supabase.Gotrue.Constants.AuthState.SignedIn || 
                    state == Supabase.Gotrue.Constants.AuthState.TokenRefreshed)
                {
                    if (Client.Auth.CurrentSession != null)
                    {
                        var persister = new CustomSessionPersistence();
                        persister.SaveSession(Client.Auth.CurrentSession);
                    }
                }
            });

            // 7. Inicializar sesiones y conectar Realtime
            await Client.InitializeAsync();
        }

        private static void InjectHttpClient(object? obj)
        {
            if (obj == null) return;
            var type = obj.GetType();

            // Reemplazamos cualquier Propiedad que sea un HttpClient
            foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (prop.PropertyType == typeof(System.Net.Http.HttpClient) && prop.CanWrite)
                {
                    if (prop.GetValue(obj) is System.Net.Http.HttpClient oldClient)
                    {
                        prop.SetValue(obj, CloneHttpClientWithInterceptor(oldClient));
                    }
                }
            }

            // Reemplazamos cualquier Campo interno que sea un HttpClient
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (field.FieldType == typeof(System.Net.Http.HttpClient))
                {
                    if (field.GetValue(obj) is System.Net.Http.HttpClient oldClient)
                    {
                        field.SetValue(obj, CloneHttpClientWithInterceptor(oldClient));
                    }
                }
            }
        }

        private static System.Net.Http.HttpClient CloneHttpClientWithInterceptor(System.Net.Http.HttpClient oldClient)
        {
            var handler = new TpmBunkerInterceptor();
            var newClient = new System.Net.Http.HttpClient(handler)
            {
                BaseAddress = oldClient.BaseAddress,
                Timeout = oldClient.Timeout
            };

            foreach (var header in oldClient.DefaultRequestHeaders)
            {
                newClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            return newClient;
        }

        public static bool IsUserAuthenticated()
        {
            if (Client == null)
            {
                return false;
            }

            return Client.Auth.CurrentSession != null;
        }
    }

    // Clase para guardar la sesión cifrada usando el chip TPM 2.0 (Nivel Bancario)
    public class CustomSessionPersistence : IGotrueSessionPersistence<Supabase.Gotrue.Session>
    {
        private const string CacheFile = "agora_session_bunker.dat";

        public void SaveSession(Supabase.Gotrue.Session session)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(session);
            AgoraBunker.BlindarPayloadSync(CacheFile, json);
        }

        public Supabase.Gotrue.Session? LoadSession()
        {
            string? json = AgoraBunker.ExtraerPayloadSync(CacheFile);
            if (!string.IsNullOrEmpty(json))
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<Supabase.Gotrue.Session>(json);
            }
            return null;
        }

        public void DestroySession()
        {
            AgoraBunker.BorrarPayloadSync(CacheFile);
        }
    }
}
