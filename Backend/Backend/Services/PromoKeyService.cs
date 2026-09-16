using System.Security.Cryptography;
using System.Text;

namespace Backend.Services
{
    public static class PromoKeyService
    {
        private const int SaltSize = 16;

        /// <summary>
        /// Генерирует открытый ключ-промокод на основе двух GUID и HMAC-SHA512.
        /// Один GUID используется в качестве ключа HMAC, а второй — в качестве данных.
        /// Возвращает 128-символьную hex-строку.
        /// </summary>
        public static string GenerateRawKey()
        {
            var keyGuid = Guid.NewGuid();
            var dataGuid = Guid.NewGuid();

            using var hmac = new HMACSHA512(keyGuid.ToByteArray());
            byte[] hashBytes = hmac.ComputeHash(dataGuid.ToByteArray());

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Создаёт криптографическую соль и хеширует открытый ключ для безопасного сохранения в БД.
        /// </summary>
        public static (string Salt, string KeyHash) HashKey(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                throw new ArgumentException("Ключ не может быть пустым.", nameof(rawKey));

            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            string salt = Convert.ToBase64String(saltBytes);

            using var hmac = new HMACSHA256(saltBytes);
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawKey.Trim().ToLowerInvariant()));
            string keyHash = Convert.ToBase64String(hashBytes);

            return (salt, keyHash);
        }

        /// <summary>
        /// Проверяет совпадение введённого ключа с сохранённым хешем и солью.
        /// Использует сравнение за фиксированное время для защиты от timing-атак.
        /// </summary>
        public static bool VerifyKey(string rawKey, string salt, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(rawKey) || string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(expectedHash))
                return false;

            try
            {
                byte[] saltBytes = Convert.FromBase64String(salt);
                byte[] expectedHashBytes = Convert.FromBase64String(expectedHash);

                using var hmac = new HMACSHA256(saltBytes);
                byte[] actualHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawKey.Trim().ToLowerInvariant()));

                return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}
