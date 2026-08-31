using Backend.Services;
using Xunit;

namespace Backend.Tests.Auth
{
    public class PasswordSecurityTests
    {
        [Fact]
        public void HashPassword_GeneratesValidHashAndSaltFormat()
        {
            // Arrange
            string password = "StrongPassword123!";

            // Act
            string hash = PasswordSecurity.HashPassword(password);

            // Assert
            Assert.NotNull(hash);
            Assert.Contains(".", hash);
            var parts = hash.Split('.');
            Assert.Equal(2, parts.Length);
            Assert.False(string.IsNullOrWhiteSpace(parts[0]));
            Assert.False(string.IsNullOrWhiteSpace(parts[1]));
        }

        [Theory]
        [InlineData("SecretPass1")]
        [InlineData("SuperComplex#2026_Key!")]
        [InlineData("Simple1")]
        public void VerifyPassword_CorrectPassword_ReturnsTrue(string password)
        {
            // Arrange
            string hash = PasswordSecurity.HashPassword(password);

            // Act
            bool isValid = PasswordSecurity.VerifyPassword(password, hash);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            // Arrange
            string hash = PasswordSecurity.HashPassword("CorrectPassword123");

            // Act
            bool isValid = PasswordSecurity.VerifyPassword("WrongPassword123", hash);

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null, "somehash")]
        [InlineData("", "somehash")]
        [InlineData("pass", null)]
        [InlineData("pass", "")]
        public void VerifyPassword_NullOrEmptyInputs_ReturnsFalse(string? password, string? hash)
        {
            // Act
            bool isValid = PasswordSecurity.VerifyPassword(password!, hash!);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void CreateHashAndSalt_And_VerifyPasswordWithSalt_WorkCorrectly()
        {
            // Arrange
            string password = "AdminPassword2026!";
            var (salt, hash) = PasswordSecurity.CreateHashAndSalt(password);

            // Act
            bool isCorrect = PasswordSecurity.VerifyPasswordWithSalt(password, salt, hash);
            bool isWrong = PasswordSecurity.VerifyPasswordWithSalt("WrongPass", salt, hash);

            // Assert
            Assert.True(isCorrect);
            Assert.False(isWrong);
        }

        [Fact]
        public void NeedsUpgrade_DetectsLegacyHashesWithoutDot()
        {
            // Arrange
            string modernHash = PasswordSecurity.HashPassword("TestPass");
            string legacyHash = "d033e22ae348aeb5660fc2140aec35850c4da997";

            // Act & Assert
            Assert.False(PasswordSecurity.NeedsUpgrade(modernHash));
            Assert.True(PasswordSecurity.NeedsUpgrade(legacyHash));
        }
    }
}
