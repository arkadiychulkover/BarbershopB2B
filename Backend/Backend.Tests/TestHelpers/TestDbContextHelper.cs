using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Backend.Tests.TestHelpers
{
    public static class TestDbContextHelper
    {
        public static AppDbContext CreateInMemoryDbContext(string dbName = "")
        {
            if (string.IsNullOrWhiteSpace(dbName))
                dbName = Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        public static BarbershopOwner CreateValidOwner(
            Guid? id = null,
            string email = "test@example.com",
            string ownerName = "Test Owner",
            string phone = "+380501112233",
            string password = "TestPassword123!",
            OwnerStatus status = OwnerStatus.Active,
            DateTime? nextPayment = null)
        {
            return new BarbershopOwner
            {
                Id = id ?? Guid.NewGuid(),
                Email = email,
                OwnerName = ownerName,
                PhoneNumber = phone,
                PasswordHash = PasswordSecurity.HashPassword(password),
                BarbershopName = "Default Barbershop",
                BarbershopAddress = "Default Address",
                BarbershopDescription = "Default Description",
                BotToken = "default_bot_token",
                BotUsername = "default_bot",
                TelegramId = "12345678",
                WalletAddress = "default_wallet",
                TimeZone = "Europe/Kyiv",
                Status = status,
                NextPayment = nextPayment ?? DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };
        }

        public static ControllerContext CreateControllerContextWithUser(Guid userId, string role = "Owner", string email = "test@example.com")
        {
            var claims = new List<Claim>
            {
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, email)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            return new ControllerContext
            {
                HttpContext = httpContext
            };
        }
    }
}
