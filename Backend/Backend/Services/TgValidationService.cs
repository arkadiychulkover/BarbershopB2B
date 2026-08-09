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

        public bool ValidateInitData(string initData, string botToken)
        {
            if (string.IsNullOrEmpty(initData) || string.IsNullOrEmpty(botToken))
                return false;

            var parsed = HttpUtility.ParseQueryString(initData);
            var hash = parsed["hash"];
            if (string.IsNullOrEmpty(hash))
                return false;

            var dataCheckString = string.Join("\n",
                parsed.AllKeys
                      .Where(k => k != "hash")
                      .OrderBy(k => k, StringComparer.Ordinal)
                      .Select(k => $"{k}={parsed[k]}"));

            var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
            var computedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

            return Convert.ToHexString(computedHash).Equals(hash, StringComparison.OrdinalIgnoreCase);
        }

        public TelegramUser? GetUserFromInitData(string initData)
        {
            var parsed = HttpUtility.ParseQueryString(initData);
            var userJson = parsed["user"];

            if (string.IsNullOrEmpty(userJson))
                return null;

            return JsonSerializer.Deserialize<TelegramUser>(userJson);
        }
    }
}
