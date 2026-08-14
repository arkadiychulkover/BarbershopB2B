using Backend.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Backend.Services
{
    public class TgValidationService
    {
        public TgValidationService() { }

        public bool ValidateInitData(string initData, string botToken, int maxAgeSeconds = 86400)
        {
            if (string.IsNullOrEmpty(initData) || string.IsNullOrEmpty(botToken))
                return false;

            var parsed = HttpUtility.ParseQueryString(initData);
            var hash = parsed["hash"];
            if (string.IsNullOrEmpty(hash))
                return false;

            // Replay attack prevention: validate auth_date within acceptable TTL
            if (!long.TryParse(parsed["auth_date"], out var authDate))
                return false;

            var currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Reject if token is older than maxAgeSeconds or more than 60s in the future (clock skew)
            if (currentUnixTime - authDate > maxAgeSeconds || authDate > currentUnixTime + 60)
                return false;

            var dataCheckString = string.Join("\n",
                parsed.AllKeys
                      .Where(k => !string.IsNullOrEmpty(k) && k != "hash")
                      .OrderBy(k => k, StringComparer.Ordinal)
                      .Select(k => $"{k}={parsed[k]}"));

            var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
            var computedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

            var computedHashHex = Convert.ToHexString(computedHash).ToLowerInvariant();
            var providedHashHex = hash.ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHashHex),
                Encoding.UTF8.GetBytes(providedHashHex)
            );
        }

        public TelegramUser? GetUserFromInitData(string initData)
        {
            var parsed = HttpUtility.ParseQueryString(initData);
            var userJson = parsed["user"];

            if (string.IsNullOrEmpty(userJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<TelegramUser>(userJson);
            }
            catch
            {
                return null;
            }
        }
    }
}
