using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;

namespace Agora.Security
{
    public static class AgoraBunker
    {
        // ---------------------------------------------------------
        // P/Invoke - Importamos el escudo nativo C++
        // ---------------------------------------------------------
        [DllImport("AgoraASMShield.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool AgoraIsEnvironmentCompromised();

        [DllImport("AgoraASMShield.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AgoraProtectWindow(IntPtr hwnd);

        private const string TPM_KEY_NAME = "AgoraMasterKeyRSA_TPM";
        private const string ENCRYPTED_KEY_FILE = "AgoraBunkerPayload.dat";

        // ---------------------------------------------------------
        // 1. OBTENER O CREAR LA LLAVE MAESTRA (Nunca sale del TPM)
        // ---------------------------------------------------------
        private static RSACng GetMasterTpmKey()
        {
            var provider = new CngProvider("Microsoft Platform Crypto Provider");

            if (CngKey.Exists(TPM_KEY_NAME, provider))
            {
                var key = CngKey.Open(TPM_KEY_NAME, provider);
                return new RSACng(key);
            }
            else
            {
                var creationParams = new CngKeyCreationParameters
                {
                    Provider = provider,
                    KeyCreationOptions = CngKeyCreationOptions.None
                };
                
                // IMPORTANTE: Muchos chips TPM 2.0 de portátiles (Intel PTT / AMD fTPM) NO soportan llaves de 4096 bits.
                // Usamos 2048 bits, que es el estándar obligatorio para TPM 2.0 y sobra para encriptar la llave AES de 32 bytes.
                creationParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));
                
                var key = CngKey.Create(CngAlgorithm.Rsa, TPM_KEY_NAME, creationParams);
                return new RSACng(key);
            }
        }

        // ---------------------------------------------------------
        // 2. EL ENTIERRO: Recibe la clave, la encripta y la guarda
        // ---------------------------------------------------------
        public static async Task BlindarClaveAsync(string claveLimpia)
        {
            try
            {
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(claveLimpia);
                
                // 1. Generar llave AES-256 efímera y Nonce para cifrar el texto grande
                byte[] aesKey = new byte[32];
                byte[] nonce = new byte[12];
                RandomNumberGenerator.Fill(aesKey);
                RandomNumberGenerator.Fill(nonce);
                
                byte[] ciphertext = new byte[dataToEncrypt.Length];
                byte[] tag = new byte[16];
                
                using (var aes = new AesGcm(aesKey, tag.Length))
                {
                    aes.Encrypt(nonce, dataToEncrypt, ciphertext, tag);
                }
                
                // 2. Cifrar la llave AES (32 bytes) con el hardware TPM 2.0 (RSA 4096)
                // Cifrado Hardware-Bound: Solo este PC físico podrá descifrar la llave AES
                using var rsa = GetMasterTpmKey();
                byte[] encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
                
                // 3. Empaquetar todo: [LenRsaKey:4][EncAesKey:Len][Nonce:12][Tag:16][Ciphertext:...]
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                bw.Write(encryptedAesKey.Length);
                bw.Write(encryptedAesKey);
                bw.Write(nonce);
                bw.Write(tag);
                bw.Write(ciphertext);
                
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync(ENCRYPTED_KEY_FILE, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(file, ms.ToArray());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Búnker C#] Error en BlindarClave: {ex.Message}");
            }
        }

        // ---------------------------------------------------------
        // 3. LA EXTRACCIÓN: El interceptor llama a esto para obtener la clave al vuelo
        // ---------------------------------------------------------
        public static async Task<string?> ExtraerClaveDelBunkerAsync()
        {
            try
            {
                if (AgoraIsEnvironmentCompromised())
                {
                    Environment.Exit(unchecked((int)0xDEAD));
                }
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.GetItemAsync(ENCRYPTED_KEY_FILE) as StorageFile;
                if (file == null) return null;

                var buffer = await FileIO.ReadBufferAsync(file);
                byte[] fileData = new byte[buffer.Length];
                using (var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
                {
                    dataReader.ReadBytes(fileData);
                }

                using var ms = new MemoryStream(fileData);
                using var br = new BinaryReader(ms);
                
                int encryptedAesKeyLen = br.ReadInt32();
                byte[] encryptedAesKey = br.ReadBytes(encryptedAesKeyLen);
                byte[] nonce = br.ReadBytes(12);
                byte[] tag = br.ReadBytes(16);
                byte[] ciphertext = br.ReadBytes((int)(ms.Length - ms.Position));

                // 1. El TPM descifra físicamente la llave AES
                using var rsa = GetMasterTpmKey();
                byte[] aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);

                // 2. Descifrar el payload original con AES-GCM
                byte[] decryptedData = new byte[ciphertext.Length];
                using (var aes = new AesGcm(aesKey, tag.Length))
                {
                    aes.Decrypt(nonce, ciphertext, tag, decryptedData);
                }
                
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Búnker C#] Error al extraer llave: {ex.Message}");
                
                // AUTORREPARACIÓN: Si la llave en el TPM o el archivo se ha corrompido/desincronizado, purgamos todo
                try {
                    var localFolder = ApplicationData.Current.LocalFolder;
                    var file = await localFolder.GetItemAsync(ENCRYPTED_KEY_FILE) as StorageFile;
                    if (file != null) await file.DeleteAsync();

                    var provider = new CngProvider("Microsoft Platform Crypto Provider");
                    if (CngKey.Exists(TPM_KEY_NAME, provider)) {
                        var key = CngKey.Open(TPM_KEY_NAME, provider);
                        key.Delete();
                    }
                    Debug.WriteLine("[Búnker C#] El sistema se ha purgado. La llave se descargará de nuevo.");
                } catch { }

                return null;
            }
        }

        // ---------------------------------------------------------
        // FLUJO DE INICIALIZACIÓN
        // ---------------------------------------------------------
        public static async Task InicializarBunkerAsync()
        {
            // 1. Verificar integridad del entorno antes de arrancar (Capa Ring-3 C++)
            if (AgoraIsEnvironmentCompromised())
            {
                Environment.Exit(unchecked((int)0xDEAD));
            }

            // 2. Comprobar si la clave ya está resguardada (Cifrada por el TPM local)
            string? claveExistente = await ExtraerClaveDelBunkerAsync();
            if (!string.IsNullOrEmpty(claveExistente))
            {
                Debug.WriteLine("[Búnker C#] La llave ya está blindada por el TPM 2.0. Saltando verificación de red.");
                return;
            }

            // 3. Si no está, la solicitamos al servidor seguro
            Debug.WriteLine("[Búnker C#] Llave no encontrada. Solicitando al servidor...");
            
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.periodiconaranja.es/agoras/skadjgflasdfjkasdlf");
            request.Headers.Add("x-platform", "windows");
            // request.Headers.Add("x-maa-token", azureAttestationToken); 

            try
            {
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string claveLimpia = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine(claveLimpia);
                    if (!string.IsNullOrEmpty(claveLimpia))
                    {
                        // 4. Guardar la clave encriptada en disco, atada al TPM
                        await BlindarClaveAsync(claveLimpia);
                        Debug.WriteLine("[Búnker C#] ¡Sistema blindado con éxito usando CNG y TPM 2.0!");
                    }
                }
                else
                {
                    Debug.WriteLine($"[Búnker C#] Servidor denegó acceso: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Búnker C#] Error crítico de red en el búnker: {ex.Message}");
            }
        }

        // ---------------------------------------------------------
        // 4. MÉTODOS GENÉRICOS TPM (Síncronos para almacenar Sesiones/Tokens)
        // ---------------------------------------------------------
        public static void BlindarPayloadSync(string filename, string payload)
        {
            try
            {
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(payload);
                byte[] aesKey = new byte[32];
                byte[] nonce = new byte[12];
                RandomNumberGenerator.Fill(aesKey);
                RandomNumberGenerator.Fill(nonce);
                
                byte[] ciphertext = new byte[dataToEncrypt.Length];
                byte[] tag = new byte[16];
                
                using (var aes = new AesGcm(aesKey, tag.Length))
                {
                    aes.Encrypt(nonce, dataToEncrypt, ciphertext, tag);
                }
                
                using var rsa = GetMasterTpmKey();
                byte[] encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
                
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                bw.Write(encryptedAesKey.Length);
                bw.Write(encryptedAesKey);
                bw.Write(nonce);
                bw.Write(tag);
                bw.Write(ciphertext);
                
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string bunkerFolder = Path.Combine(localAppData, "AgoraApp");
                if (!Directory.Exists(bunkerFolder)) Directory.CreateDirectory(bunkerFolder);

                string filePath = Path.Combine(bunkerFolder, filename);
                File.WriteAllBytes(filePath, ms.ToArray());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Búnker C#] Error en BlindarPayloadSync: {ex.Message}");
            }
        }

        public static string? ExtraerPayloadSync(string filename)
        {
            try
            {
                if (AgoraIsEnvironmentCompromised())
                {
                    Environment.Exit(unchecked((int)0xDEAD));
                }

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string bunkerFolder = Path.Combine(localAppData, "AgoraApp");
                string filePath = Path.Combine(bunkerFolder, filename);
                if (!File.Exists(filePath)) return null;

                byte[] fileData = File.ReadAllBytes(filePath);

                using var ms = new MemoryStream(fileData);
                using var br = new BinaryReader(ms);
                
                int encryptedAesKeyLen = br.ReadInt32();
                byte[] encryptedAesKey = br.ReadBytes(encryptedAesKeyLen);
                byte[] nonce = br.ReadBytes(12);
                byte[] tag = br.ReadBytes(16);
                byte[] ciphertext = br.ReadBytes((int)(ms.Length - ms.Position));

                using var rsa = GetMasterTpmKey();
                byte[] aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);

                byte[] decryptedData = new byte[ciphertext.Length];
                using (var aes = new AesGcm(aesKey, tag.Length))
                {
                    aes.Decrypt(nonce, ciphertext, tag, decryptedData);
                }
                
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Búnker C#] Error en ExtraerPayloadSync: {ex.Message}");
                return null;
            }
        }

        public static void BorrarPayloadSync(string filename)
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string bunkerFolder = Path.Combine(localAppData, "AgoraApp");
                string filePath = Path.Combine(bunkerFolder, filename);

                if (File.Exists(filePath))
                {
                    // Destrucción segura militar: sobrescribir con ceros antes de desenlazar el archivo
                    byte[] zeros = new byte[new FileInfo(filePath).Length];
                    File.WriteAllBytes(filePath, zeros);
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Búnker C#] Error al borrar payload: {ex.Message}");
            }
        }

        // ---------------------------------------------------------
        // 5. LLAVE DE IDENTIDAD E2EE (Generada físicamente en el TPM)
        // ---------------------------------------------------------
        public static string GenerarYObtenerClavePublica()
        {
            const string IDENTITY_KEY_NAME = "AgoraIdentityKey_TPM";
            var provider = new CngProvider("Microsoft Platform Crypto Provider");

            RSACng rsa;
            if (CngKey.Exists(IDENTITY_KEY_NAME, provider))
            {
                var key = CngKey.Open(IDENTITY_KEY_NAME, provider);
                rsa = new RSACng(key);
            }
            else
            {
                var creationParams = new CngKeyCreationParameters
                {
                    Provider = provider,
                    KeyCreationOptions = CngKeyCreationOptions.None
                };
                creationParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));
                
                var key = CngKey.Create(CngAlgorithm.Rsa, IDENTITY_KEY_NAME, creationParams);
                rsa = new RSACng(key);
            }

            // Exportamos la llave pública en formato estándar SPKI (SubjectPublicKeyInfo)
            byte[] pubKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            return Convert.ToBase64String(pubKeyBytes);
        }
    }
}
