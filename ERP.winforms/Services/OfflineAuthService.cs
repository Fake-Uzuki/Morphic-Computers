using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ERP.winforms.Services
{
    public class CachedUserCredential
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SaltBase64 { get; set; } = string.Empty;
        public string HashBase64 { get; set; } = string.Empty;
        public DateTime LastLoginUtc { get; set; } = DateTime.UtcNow;
        public bool IsPOSAllowed { get; set; } = true;
        public bool IsInventoryAllowed { get; set; } = true;
        public bool IsRepairAllowed { get; set; }
        public bool IsSupplierAllowed { get; set; }
    }

    public class OfflineAuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CachedUserCredential? Credential { get; set; }

        public static OfflineAuthResult Succeeded(CachedUserCredential cred) =>
            new() { Success = true, Message = "Offline authentication successful.", Credential = cred };

        public static OfflineAuthResult Failed(string message) =>
            new() { Success = false, Message = message };
    }

    /// <summary>
    /// Enterprise-grade DPAPI-encrypted credential vault for secure offline store operations.
    /// Stores salted cryptographic hashes (PBKDF2 SHA-256, 100,000 iterations) sealed using Windows Data Protection API (DPAPI).
    /// Prevents decompilation leaks, cross-tenant credential exposure, and unauthorized offline logins.
    /// </summary>
    public class OfflineAuthService
    {
        private static OfflineAuthService? _instance;
        public static OfflineAuthService Instance => _instance ??= new OfflineAuthService();

        private readonly string _vaultFilePath;
        private readonly object _lock = new();
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MorphicErp_Vault_Salt_2026");
        private const int Pbkdf2Iterations = 100_000;
        private const int HashByteSize = 32;
        private const int SaltByteSize = 16;

        private OfflineAuthService()
        {
            string appData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _vaultFilePath = Path.Combine(appData, "auth_vault.dat");
        }

        private List<CachedUserCredential> ReadVault()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_vaultFilePath)) return new List<CachedUserCredential>();

                    byte[] encryptedBytes = File.ReadAllBytes(_vaultFilePath);
                    byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                    string json = Encoding.UTF8.GetString(decryptedBytes);
                    var list = JsonSerializer.Deserialize<List<CachedUserCredential>>(json);
                    return list ?? new List<CachedUserCredential>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ReadVault error: {ex.Message}");
                    return new List<CachedUserCredential>();
                }
            }
        }

        private void WriteVault(List<CachedUserCredential> credentials)
        {
            lock (_lock)
            {
                try
                {
                    string json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = false });
                    byte[] plainBytes = Encoding.UTF8.GetBytes(json);
                    byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(_vaultFilePath, encryptedBytes);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WriteVault error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Securely caches a user's credentials after a successful online authentication.
        /// Generates a unique cryptographic salt and PBKDF2 hash, sealing it into the DPAPI vault.
        /// </summary>
        public void CacheSuccessfulLogin(ApiClient.LoginResult loginResult, string password)
        {
            if (loginResult == null || !loginResult.Success || string.IsNullOrWhiteSpace(password)) return;

            try
            {
                byte[] salt = RandomNumberGenerator.GetBytes(SaltByteSize);
                byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Pbkdf2Iterations,
                    HashAlgorithmName.SHA256,
                    HashByteSize);

                var vault = ReadVault();

                // Remove existing entry for same company + user to update
                vault.RemoveAll(c =>
                    c.CompanyId == loginResult.CompanyId &&
                    c.Username.Equals(loginResult.Username, StringComparison.OrdinalIgnoreCase));

                vault.Add(new CachedUserCredential
                {
                    CompanyId = loginResult.CompanyId,
                    CompanyCode = loginResult.CompanyCode,
                    CompanyName = loginResult.CompanyName,
                    PlanName = loginResult.PlanName,
                    Username = loginResult.Username,
                    DisplayName = loginResult.Username,
                    Role = loginResult.Role,
                    SaltBase64 = Convert.ToBase64String(salt),
                    HashBase64 = Convert.ToBase64String(hash),
                    LastLoginUtc = DateTime.UtcNow,
                    IsPOSAllowed = loginResult.IsPOSAllowed,
                    IsInventoryAllowed = loginResult.IsInventoryAllowed,
                    IsRepairAllowed = loginResult.IsRepairAllowed,
                    IsSupplierAllowed = loginResult.IsSupplierAllowed
                });

                WriteVault(vault);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CacheSuccessfulLogin error: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates offline login against the machine's DPAPI-encrypted salted hash vault.
        /// Does not require server or internet connectivity.
        /// </summary>
        public OfflineAuthResult ValidateOfflineLogin(string companyInput, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(companyInput) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return OfflineAuthResult.Failed("Please enter Company Name, Username, and Password.");
            }

            companyInput = companyInput.Trim();
            username = username.Trim();

            var vault = ReadVault();

            // Find matching company & user in local vault
            var match = vault.FirstOrDefault(c =>
                c.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                (c.CompanyName.Equals(companyInput, StringComparison.OrdinalIgnoreCase) ||
                 c.CompanyCode.Equals(companyInput, StringComparison.OrdinalIgnoreCase)));

            if (match == null)
            {
                return OfflineAuthResult.Failed(
                    $"Offline login unavailable for '{username}'.\n\nThis account has not previously logged into this computer while connected to the internet. Please connect to the internet for the first-time setup.");
            }

            try
            {
                byte[] salt = Convert.FromBase64String(match.SaltBase64);
                byte[] expectedHash = Convert.FromBase64String(match.HashBase64);

                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Pbkdf2Iterations,
                    HashAlgorithmName.SHA256,
                    HashByteSize);

                if (CryptographicOperations.FixedTimeEquals(computedHash, expectedHash))
                {
                    return OfflineAuthResult.Succeeded(match);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValidateOfflineLogin error: {ex.Message}");
            }

            return OfflineAuthResult.Failed("Invalid username or password. Please try again.");
        }
    }
}
