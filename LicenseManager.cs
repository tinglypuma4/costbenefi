using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace costbenefi.Models  // ← Namespace correcto para tu proyecto
{
    public class LicenseManager
    {
        private const string LICENSE_FILE = "license.key";
        // MISMA CLAVE que en Python (CAMBIAR EN PRODUCCIÓN)
        private const string SECRET_KEY = "TU-CLAVE-SECRETA-SUPER-COMPLEJA-2025";

        public enum LicenseType
        {
            BASICA,     // 1 año, 1000 ventas
            MEDIA,      // 2 años, 5000 ventas
            AVANZADA,   // 3 años, 20000 ventas
            PORVIDA     // 100 años, ilimitadas
        }

        public class LicenseInfo
        {
            public bool IsValid { get; set; }
            public LicenseType Type { get; set; }
            public DateTime ExpirationDate { get; set; }
            public string CompanyName { get; set; }
            public int MaxTransactions { get; set; }
            public string Message { get; set; }
            public int DaysRemaining { get; set; }
        }

        /// <summary>
        /// Validar licencia al iniciar la aplicación
        /// </summary>
        public static LicenseInfo ValidateLicense()
        {
            try
            {
                if (!File.Exists(LICENSE_FILE))
                {
                    return new LicenseInfo
                    {
                        IsValid = false,
                        Message = "No se encontró archivo de licencia. El sistema requiere activación."
                    };
                }

                string encryptedKey = File.ReadAllText(LICENSE_FILE).Trim();

                if (string.IsNullOrWhiteSpace(encryptedKey))
                {
                    return new LicenseInfo
                    {
                        IsValid = false,
                        Message = "Archivo de licencia vacío o corrupto."
                    };
                }

                string decryptedKey = DecryptLicenseKey(encryptedKey);
                return ParseLicenseKey(decryptedKey);
            }
            catch (Exception ex)
            {
                return new LicenseInfo
                {
                    IsValid = false,
                    Message = $"Error al validar licencia: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Parsear y validar la licencia desencriptada
        /// Formato: TIPO-EMPRESA-FECHA-TRANSACCIONES-HASH
        /// Ejemplo: BASICA-MiEmpresa-20261231-1000-A1B2C3D4
        /// </summary>
        private static LicenseInfo ParseLicenseKey(string key)
        {
            try
            {
                var parts = key.Split('-');

                if (parts.Length < 5)
                {
                    return new LicenseInfo
                    {
                        IsValid = false,
                        Message = "Formato de licencia inválido"
                    };
                }

                string tipo = parts[0];
                string empresa = parts[1].Replace("_", " ");
                string fechaStr = parts[2];
                string ventasStr = parts[3];
                string hashRecibido = parts[4];

                // Verificar hash de integridad
                string dataToHash = $"{tipo}-{parts[1]}-{fechaStr}-{ventasStr}";
                string expectedHash = GenerateHash(dataToHash).Substring(0, 8);

                if (hashRecibido != expectedHash)
                {
                    return new LicenseInfo
                    {
                        IsValid = false,
                        Message = "Licencia corrupta o modificada. Hash inválido."
                    };
                }

                // Parsear tipo de licencia
                if (!Enum.TryParse<LicenseType>(tipo, true, out var licenseType))
                {
                    return new LicenseInfo
                    {
                        IsValid = false,
                        Message = $"Tipo de licencia desconocido: {tipo}"
                    };
                }

                // Parsear fecha de expiración (formato: YYYYMMDD)
                if (!DateTime.TryParseExact(fechaStr, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out var expirationDate))
                {
                    return new LicenseInfo
                    {
                        IsValid = false,
                        Message = "Fecha de expiración inválida"
                    };
                }

                // Parsear límite de transacciones
                int maxTransactions;
                if (ventasStr == "UNLIMITED")
                {
                    maxTransactions = int.MaxValue;
                }
                else if (!int.TryParse(ventasStr, out maxTransactions))
                {
                    return new LicenseInfo
                    {
                        IsValid = false,
                        Message = "Límite de transacciones inválido"
                    };
                }

                // Verificar si expiró (solo para licencias no perpetuas)
                int daysRemaining = (expirationDate - DateTime.Now).Days;

                if (DateTime.Now > expirationDate && licenseType != LicenseType.PORVIDA)
                {
                    return new LicenseInfo
                    {
                        IsValid = false,
                        Type = licenseType,
                        ExpirationDate = expirationDate,
                        CompanyName = empresa,
                        DaysRemaining = daysRemaining,
                        Message = $"La licencia expiró el {expirationDate:dd/MM/yyyy}"
                    };
                }

                // Licencia válida
                return new LicenseInfo
                {
                    IsValid = true,
                    Type = licenseType,
                    ExpirationDate = expirationDate,
                    CompanyName = empresa,
                    MaxTransactions = maxTransactions,
                    DaysRemaining = daysRemaining,
                    Message = $"Licencia {licenseType} válida hasta {expirationDate:dd/MM/yyyy}"
                };
            }
            catch (Exception ex)
            {
                return new LicenseInfo
                {
                    IsValid = false,
                    Message = $"Error al procesar licencia: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Desencriptar licencia usando AES (compatible con Python)
        /// </summary>
        private static string DecryptLicenseKey(string cipherText)
        {
            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Usar SHA256 del SECRET_KEY como clave (igual que Python)
                    byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(SECRET_KEY));
                    aes.Key = key;

                    // Extraer IV (primeros 16 bytes)
                    byte[] iv = new byte[16];
                    Array.Copy(buffer, 0, iv, 0, 16);
                    aes.IV = iv;

                    // Desencriptar los datos restantes
                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(buffer, 16, buffer.Length - 16))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al desencriptar licencia: {ex.Message}");
            }
        }

        /// <summary>
        /// Generar hash SHA256 (compatible con Python)
        /// </summary>
        private static string GenerateHash(string input)
        {
            string combined = input + SECRET_KEY;
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
            return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }

        /// <summary>
        /// Verificar si se ha alcanzado el límite de transacciones
        /// </summary>
        public static bool CheckTransactionLimit(LicenseInfo license, int currentTransactionCount)
        {
            if (!license.IsValid)
                return false;

            if (license.MaxTransactions == int.MaxValue)
                return true;

            return currentTransactionCount < license.MaxTransactions;
        }

        /// <summary>
        /// Guardar nueva licencia en archivo
        /// </summary>
        public static bool SaveLicense(string encryptedLicenseKey)
        {
            try
            {
                File.WriteAllText(LICENSE_FILE, encryptedLicenseKey.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtener descripción del tipo de licencia
        /// </summary>
        public static string GetLicenseTypeDescription(LicenseType type)
        {
            return type switch
            {
                LicenseType.BASICA => "Licencia Básica - 1 año, hasta 1,000 ventas",
                LicenseType.MEDIA => "Licencia Media - 2 años, hasta 5,000 ventas",
                LicenseType.AVANZADA => "Licencia Avanzada - 3 años, hasta 20,000 ventas",
                LicenseType.PORVIDA => "Licencia de Por Vida - Ilimitada",
                _ => "Desconocida"
            };
        }

        /// <summary>
        /// Verificar si la licencia está por vencer (menos de 30 días)
        /// </summary>
        public static bool IsExpiringSoon(LicenseInfo license)
        {
            if (!license.IsValid || license.Type == LicenseType.PORVIDA)
                return false;

            return license.DaysRemaining <= 30 && license.DaysRemaining > 0;
        }
    }
}