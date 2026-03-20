using System.Security.Cryptography;

namespace ITExam.Services
{
    public static class PasswordHasherUtil
    {
        private const int Iterations = 200_000;
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;

        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(HashSizeBytes);

            return $"v1|{Iterations}|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            if (password == null) return false;

            var parts = storedHash.Split('|');
            if (parts.Length != 4) return false;
            if (!string.Equals(parts[0], "v1", StringComparison.OrdinalIgnoreCase)) return false;

            if (!int.TryParse(parts[1], out var iterations)) return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var actual = pbkdf2.GetBytes(expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}

