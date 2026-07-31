using Agora.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agora.Security
{
    public static class AgoraKeyManager
    {
        private const string KeyName = "AgoraIdentityKey";

        // Obtiene la instancia RSA persistida de forma nativa en Windows (CAPI/CNG)
        // Esto es el equivalente directo al AndroidKeyStore. La clave privada es inextraíble.
        private static RSA GetDevicePrivateKey()
        {
            if (CngKey.Exists(KeyName))
            {
                var key = CngKey.Open(KeyName);
                return new RSACng(key);
            }
            else
            {
                var creationParams = new CngKeyCreationParameters
                {
                    ExportPolicy = CngExportPolicies.None, // Hardware bound / Non-exportable
                    KeyCreationOptions = CngKeyCreationOptions.None
                };
                creationParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));
                
                var key = CngKey.Create(CngAlgorithm.Rsa, KeyName, creationParams);
                return new RSACng(key);
            }
        }

        public static string GenerarYObtenerClavePublica()
        {
            using var rsa = GetDevicePrivateKey();
            // Exportar clave pública en formato SubjectPublicKeyInfo (X.509)
            byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            // Convertir a Base64 sin saltos de línea
            return Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.None);
        }

        private static RSA GetPublicKeyFromString(string base64PublicKey)
        {
            byte[] keyBytes = Convert.FromBase64String(base64PublicKey);
            var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            return rsa;
        }

        // ========================================================================
        // 🔥 CORE E2EE: CREACIÓN DE GRUPOS Y CRIPTOGRAFÍA DE MENSAJES 🔥
        // ========================================================================

        public static string CrearGrupoYDistribuir(string dispositivosJsonStr)
        {
            try
            {
                // A. Creamos la llave maestra irrepetible del chat (AES-256)
                byte[] aesBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(aesBytes);
                }

                using var jsonDoc = JsonDocument.Parse(dispositivosJsonStr);
                var jsonOutput = new List<Dictionary<string, string>>();

                // B. Para cada amigo del grupo (y nosotros):
                foreach (var item in jsonDoc.RootElement.EnumerateArray())
                {
                    string deviceId = item.GetProperty("device_identifier").GetString()!;
                    string pubKeyBase64 = item.GetProperty("public_device_key").GetString()!;

                    using var rsa = GetPublicKeyFromString(pubKeyBase64);
                    // Ciframos usando RSA-OAEP-SHA256 igual que en Kotlin (OAEPWithSHA-256AndMGF1Padding)
                    byte[] encryptedKeyBytes = rsa.Encrypt(aesBytes, RSAEncryptionPadding.OaepSHA256);
                    string encryptedKeyBase64 = Convert.ToBase64String(encryptedKeyBytes, Base64FormattingOptions.None);

                    jsonOutput.Add(new Dictionary<string, string>
                    {
                        { "device_id", deviceId },
                        { "encrypted_key", encryptedKeyBase64 }
                    });
                }

                return JsonSerializer.Serialize(jsonOutput);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bunker C#] Error creando llaves de grupo: {ex.Message}");
                return "[]";
            }
        }

        public static byte[]? DescifrarLlaveDeChat(string paqueteRSA_Base64)
        {
            try
            {
                byte[] bytesCifrados = Convert.FromBase64String(paqueteRSA_Base64);
                using var rsa = GetDevicePrivateKey();
                // Abrimos el candado usando la llave privada nativa de Windows
                return rsa.Decrypt(bytesCifrados, RSAEncryptionPadding.OaepSHA256);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bunker C#] No se pudo romper el cifrado de la llave: {ex.Message}");
                return null;
            }
        }

        public static string? CifrarMensajeTexto(string textoPlano, byte[] aesKey)
        {
            try
            {
                byte[] plaintextBytes = Encoding.UTF8.GetBytes(textoPlano);
                byte[] nonce = new byte[12]; // IV único requerido por GCM
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(nonce);

                byte[] ciphertext = new byte[plaintextBytes.Length];
                byte[] tag = new byte[16];

                using (var aesGcm = new AesGcm(aesKey, 16))
                {
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }

                // En Java, Cipher.doFinal devuelve el ciphertext concatenado con el tag de autenticación.
                // Replicamos esta concatenación en C# para garantizar 100% de compatibilidad.
                byte[] cipherWithTag = new byte[ciphertext.Length + tag.Length];
                Buffer.BlockCopy(ciphertext, 0, cipherWithTag, 0, ciphertext.Length);
                Buffer.BlockCopy(tag, 0, cipherWithTag, ciphertext.Length, tag.Length);

                string ivStr = Convert.ToBase64String(nonce, Base64FormattingOptions.None);
                string bodyStr = Convert.ToBase64String(cipherWithTag, Base64FormattingOptions.None);

                return $"{ivStr}:{bodyStr}";
            }
            catch
            {
                return null;
            }
        }

        public static string? DescifrarMensajeTexto(string mensajeIninteligible, byte[] aesKey)
        {
            try
            {
                var partes = mensajeIninteligible.Split(':');
                if (partes.Length != 2) return null;

                byte[] nonce = Convert.FromBase64String(partes[0]);
                byte[] cipherWithTag = Convert.FromBase64String(partes[1]);

                if (cipherWithTag.Length < 16) return null; // Debe contener al menos el Tag

                byte[] ciphertext = new byte[cipherWithTag.Length - 16];
                byte[] tag = new byte[16];

                // Extraemos ciphertext y tag por separado
                Buffer.BlockCopy(cipherWithTag, 0, ciphertext, 0, ciphertext.Length);
                Buffer.BlockCopy(cipherWithTag, ciphertext.Length, tag, 0, 16);

                byte[] plaintextBytes = new byte[ciphertext.Length];

                using (var aesGcm = new AesGcm(aesKey, 16))
                {
                    aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
                }

                return Encoding.UTF8.GetString(plaintextBytes);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("[Bunker C#] Error GCM descifrando mensaje");
                return null;
            }
        }

        // ========================================================================
        // REGISTRO DE DISPOSITIVO EN SUPABASE
        // ========================================================================
        
        public static async Task RegistrarDispositivoAsync(string userId)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            
            // 1. Cargar o crear un identificador local único
            if (!localSettings.Values.TryGetValue("agora_device_identifier", out object? val) || !(val is string deviceId))
            {
                deviceId = "win_" + Guid.NewGuid().ToString("N");
                localSettings.Values["agora_device_identifier"] = deviceId;
            }

            // 2. Extraer la Clave Pública asimétrica
            string pubKeyBase64 = GenerarYObtenerClavePublica();

            // 3. Obtener el Token WNS (Windows Push Notification Services)
            string wnsToken = "";
            try
            {
                var channel = await Windows.Networking.PushNotifications.PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                wnsToken = channel.Uri;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bunker C#] No se pudo obtener el token WNS (¿Falta asociar la app a la Store?): {ex.Message}");
                wnsToken = "WNS_TOKEN_PENDIENTE";
            }

            var deviceData = new Devices
            {
                DeviceIdentifier = deviceId,
                UserId = userId,
                FcmToken = wnsToken, // Usamos la misma columna para almacenar el WNS (APNs en iOS)
                PublicDeviceKey = pubKeyBase64,
                DeviceName = Environment.MachineName,
                Platform = "windows",
                LastSeen = DateTime.UtcNow
            };

            var supabase = Services.SupabaseConnect.Client;
            if (supabase == null) throw new InvalidOperationException("Supabase no está inicializado.");

            // 3. Upsert en la tabla devices
            var response = await supabase.From<Devices>().Upsert(deviceData);
            var insertedDevice = response.Models.FirstOrDefault();
            
            if (insertedDevice?.Id != null)
            {
                // Guardamos el UUID devuelto por Postgres
                localSettings.Values["agora_device_db_id"] = insertedDevice.Id;
                System.Diagnostics.Debug.WriteLine($"[Bunker C#] ✅ Hardware vinculado con éxito. UUID DB: {insertedDevice.Id}");
            }
        }
    }
}
