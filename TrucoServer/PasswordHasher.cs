using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
public static class PasswordHasher
{
    private const int Iterations = 10000;

    public static string Hash(string password)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, 16, Iterations, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(32);
            byte[] salt = pbkdf2.Salt;

            // Combina el salt y el hash para almacenarlos juntos
            byte[] hashBytes = new byte[48];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, 16);
            Buffer.BlockCopy(hash, 0, hashBytes, 16, 32);

            // Convierte a cadena Base64
            return Convert.ToBase64String(hashBytes);
        }
    }

    public static bool Verify(string password, string storedHash)
    {
        byte[] hashBytes = Convert.FromBase64String(storedHash);

        // Extrae el salt (los primeros 16 bytes)
        byte[] salt = new byte[16];
        Buffer.BlockCopy(hashBytes, 0, salt, 0, 16);

        // Extrae el hash original (los 32 bytes siguientes)
        byte[] storedPasswordHash = new byte[32];
        Buffer.BlockCopy(hashBytes, 16, storedPasswordHash, 0, 32);

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
        {
            byte[] newHash = pbkdf2.GetBytes(32);

            // Compara byte a byte
            return newHash.SequenceEqual(storedPasswordHash);
        }
    }
}