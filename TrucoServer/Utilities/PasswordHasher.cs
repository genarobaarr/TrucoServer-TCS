using System;
using System.Linq;
using System.Security.Cryptography;

namespace TrucoServer.Utilities
{
    public static class PasswordHasher
    {
        private const int ITERATIONS = 310000;
        private const int SALT_SIZE = 16;
        private const int HASH_SIZE = 32;
        private const int INITIAL_OFFSET_SIZE = 0;
        private const int TOTAL_HASH_SIZE = SALT_SIZE + HASH_SIZE;

        public static string Hash(string password)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, SALT_SIZE, ITERATIONS, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HASH_SIZE);
                byte[] salt = pbkdf2.Salt;
                byte[] hashBytes = new byte[TOTAL_HASH_SIZE];
                Buffer.BlockCopy(salt, INITIAL_OFFSET_SIZE, hashBytes, INITIAL_OFFSET_SIZE, SALT_SIZE);
                Buffer.BlockCopy(hash, INITIAL_OFFSET_SIZE, hashBytes, SALT_SIZE, HASH_SIZE);

                return Convert.ToBase64String(hashBytes);
            }
        }

        public static bool Verify(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            byte[] salt = new byte[SALT_SIZE];
            Buffer.BlockCopy(hashBytes, INITIAL_OFFSET_SIZE, salt, INITIAL_OFFSET_SIZE, SALT_SIZE);
            byte[] storedPasswordHash = new byte[HASH_SIZE];
            Buffer.BlockCopy(hashBytes, SALT_SIZE, storedPasswordHash, INITIAL_OFFSET_SIZE, HASH_SIZE);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA256))
            {
                byte[] newHash = pbkdf2.GetBytes(HASH_SIZE);

                return newHash.SequenceEqual(storedPasswordHash);
            }
        }
    }
}