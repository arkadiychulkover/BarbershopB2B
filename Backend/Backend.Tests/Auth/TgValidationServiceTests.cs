using System.Security.Cryptography;
using System.Text;
using Backend.Services;
using Xunit;

namespace Backend.Tests.Auth
{
    public class TgValidationServiceTests
    {
        private readonly TgValidationService _validationService;

        public TgValidationServiceTests()
        {
            _validationService = new TgValidationService();
        }

        [Fact]
        public void ValidateInitData_ValidInitDataAndSignature_ReturnsTrue()
        {
            // Arrange
            string botToken = "123456789:ABCdefGHIjklMNOpqrsTUVwxyz";
            long authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string userJson = "{\"id\":12345678,\"first_name\":\"Ivan\",\"username\":\"ivan_barber\"}";

            // Build data check string according to Telegram specification
            // keys sorted alphabetically, omitting hash
            string dataCheckString = $"auth_date={authDate}\nuser={userJson}";

            var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
            var computedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
            string validHash = Convert.ToHexString(computedHash).ToLowerInvariant();

            string initData = $"auth_date={authDate}&user={Uri.EscapeDataString(userJson)}&hash={validHash}";

            // Act
            bool isValid = _validationService.ValidateInitData(initData, botToken);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateInitData_TamperedData_ReturnsFalse()
        {
            // Arrange
            string botToken = "123456789:ABCdefGHIjklMNOpqrsTUVwxyz";
            long authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string userJson = "{\"id\":12345678,\"first_name\":\"Ivan\"}";

            string dataCheckString = $"auth_date={authDate}\nuser={userJson}";
            var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
            var computedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
            string validHash = Convert.ToHexString(computedHash).ToLowerInvariant();

            // Tamper user ID
            string tamperedUserJson = "{\"id\":99999999,\"first_name\":\"Ivan\"}";
            string initData = $"auth_date={authDate}&user={Uri.EscapeDataString(tamperedUserJson)}&hash={validHash}";

            // Act
            bool isValid = _validationService.ValidateInitData(initData, botToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateInitData_ExpiredAuthDate_ReturnsFalse()
        {
            // Arrange
            string botToken = "123456789:ABCdefGHIjklMNOpqrsTUVwxyz";
            // 2 days ago
            long oldAuthDate = DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeSeconds();
            string userJson = "{\"id\":12345678}";

            string dataCheckString = $"auth_date={oldAuthDate}\nuser={userJson}";
            var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
            var computedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
            string hash = Convert.ToHexString(computedHash).ToLowerInvariant();

            string initData = $"auth_date={oldAuthDate}&user={Uri.EscapeDataString(userJson)}&hash={hash}";

            // Act
            bool isValid = _validationService.ValidateInitData(initData, botToken, maxAgeSeconds: 86400);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GetUserFromInitData_ValidUserJson_ParsesSuccessfully()
        {
            // Arrange
            string userJson = "{\"id\":987654321,\"first_name\":\"Alex\",\"last_name\":\"Smith\",\"username\":\"asmith\"}";
            string initData = $"auth_date=1700000000&user={Uri.EscapeDataString(userJson)}&hash=dummy";

            // Act
            var user = _validationService.GetUserFromInitData(initData);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(987654321, user.Id);
            Assert.Equal("Alex", user.FirstName);
            Assert.Equal("Smith", user.LastName);
            Assert.Equal("asmith", user.Username);
        }
    }
}
