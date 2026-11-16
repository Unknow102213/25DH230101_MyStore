using System;
using System.Security.Cryptography;

namespace MyStore.Models.Security
{
    public static class PasswordHasher
    {
        private const int Iterations = 10000;
        public static string Hash(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                byte[] hash = derive.GetBytes(32);
                return "PBKDF2$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }
        public static bool Verify(string password, string stored)
        {
            if (String.IsNullOrEmpty(stored)) return false;
            if (!stored.StartsWith("PBKDF2$", StringComparison.Ordinal)) return String.Equals(password, stored.TrimEnd(), StringComparison.Ordinal);
            try
            {
                string[] parts = stored.Split('$');
                int iterations = Int32.Parse(parts[1]);
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
                {
                    byte[] actual = derive.GetBytes(expected.Length);
                    int diff = 0;
                    for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ actual[i];
                    return diff == 0;
                }
            }
            catch { return false; }
        }
    }
}
