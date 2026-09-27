using System;
using System.Security.Cryptography;
using System.Text;

namespace ERP.domain.security
{
    /// <summary>
    /// Cryptographic helper for secure password hashing and verification.
    /// Uses SHA-256 cryptographic hashing to prevent storing or exposing plain text credentials.
    /// </summary>
    public static class PasswordHelper
    {
        public static string HashPassword(string? rawPassword)
        {
            if (string.IsNullOrEmpty(rawPassword))
            {
                return string.Empty;
            }

            byte[] inputBytes = Encoding.UTF8.GetBytes(rawPassword);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public static bool VerifyPassword(string? rawPassword, string? hashedPassword)
        {
            if (string.IsNullOrEmpty(rawPassword) || string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            string computed = HashPassword(rawPassword);
            byte[] a = Encoding.UTF8.GetBytes(computed);
            byte[] b = Encoding.UTF8.GetBytes(hashedPassword);

            if (a.Length != b.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
