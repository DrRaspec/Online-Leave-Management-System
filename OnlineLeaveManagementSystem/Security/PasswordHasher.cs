using System;
using System.Security.Cryptography;

namespace OnlineLeaveManagementSystem.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string GenerateSalt()
        {
            byte[] salt = new byte[SaltSize];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password ?? string.Empty, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(deriveBytes.GetBytes(HashSize));
            }
        }

        public static bool VerifyPassword(string password, string salt, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(expectedHash))
            {
                return false;
            }

            string actualHash = HashPassword(password, salt);
            return FixedTimeEquals(actualHash, expectedHash);
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            byte[] leftBytes = Convert.FromBase64String(left);
            byte[] rightBytes = Convert.FromBase64String(right);

            if (leftBytes.Length != rightBytes.Length)
            {
                return false;
            }

            int diff = 0;
            for (int index = 0; index < leftBytes.Length; index++)
            {
                diff |= leftBytes[index] ^ rightBytes[index];
            }

            return diff == 0;
        }
    }
}
