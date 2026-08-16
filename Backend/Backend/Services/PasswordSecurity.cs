using System.Security.Cryptography;
using System.Text;

namespace Backend.Services
{
    public static class PasswordSecurity
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static (string Salt, string Hash) CreateHashAndSalt(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );
            return (Convert.ToBase64String(saltBytes), Convert.ToBase64String(hashBytes));
        }

        public static bool VerifyPasswordWithSalt(string password, string salt, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                byte[] saltBytes = Convert.FromBase64String(salt);
                byte[] expectedHashBytes = Convert.FromBase64String(hash);

                byte[] actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    saltBytes,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    HashSize
                );

                return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
            }
            catch
            {
                return false;
            }
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            var parts = storedHash.Split('.');
            if (parts.Length == 2)
            {
                try
                {
                    byte[] salt = Convert.FromBase64String(parts[0]);
                    byte[] expectedHash = Convert.FromBase64String(parts[1]);

                    byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                        password,
                        salt,
                        Iterations,
                        HashAlgorithmName.SHA256,
                        HashSize
                    );

                    return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                using var sha256 = SHA256.Create();
                var legacyHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(legacyHash),
                    Encoding.UTF8.GetBytes(storedHash)
                );
            }
            catch
            {
                return false;
            }
        }

        public static bool NeedsUpgrade(string storedHash)
        {
            return !storedHash.Contains('.');
        }
    }
}
