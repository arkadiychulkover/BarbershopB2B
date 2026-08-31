using Backend.Controllers;
using Backend.DTOs;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;


namespace Backend.Tests.Auth
{
    public class RegestrationControllerTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IEmailService> _emailServiceMock;

        public RegestrationControllerTests()
        {
            _configMock = new Mock<IConfiguration>();
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x.Value).Returns("SuperSecretJwtKeyWithSufficientLengthForHmacSha256Security123456!");
            
            var issuerSection = new Mock<IConfigurationSection>();
            issuerSection.Setup(x => x.Value).Returns("https://backendbarbershopdomen.online");

            var audienceSection = new Mock<IConfigurationSection>();
            audienceSection.Setup(x => x.Value).Returns("https://arch-shop.store");

            var jwtParentSection = new Mock<IConfigurationSection>();
            jwtParentSection.Setup(x => x.GetSection("Secret")).Returns(jwtSection.Object);
            jwtParentSection.Setup(x => x["Secret"]).Returns("SuperSecretJwtKeyWithSufficientLengthForHmacSha256Security123456!");
            jwtParentSection.Setup(x => x.GetSection("Issuer")).Returns(issuerSection.Object);
            jwtParentSection.Setup(x => x["Issuer"]).Returns("https://backendbarbershopdomen.online");
            jwtParentSection.Setup(x => x.GetSection("Audience")).Returns(audienceSection.Object);
            jwtParentSection.Setup(x => x["Audience"]).Returns("https://arch-shop.store");

            _configMock.Setup(c => c.GetSection("JwtSettings")).Returns(jwtParentSection.Object);
            _configMock.Setup(c => c["FrontendUrl"]).Returns("http://localhost:5173");

            _emailServiceMock = new Mock<IEmailService>();
        }

        [Fact]
        public async Task Register_ValidOwner_ReturnsOkAndCreatesUser()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var controller = new RegestrationController(context, _configMock.Object, _emailServiceMock.Object);

            var request = new RegistrationRequest
            {
                Email = "NewOwner@test.com",
                OwnerName = "John Doe",
                PhoneNumber = "+380991234567",
                Password = "Password123!",
                BarbershopName = "Elite Cuts",
                BarbershopAddress = "Main St 10",
                BarbershopDescription = "Best cuts",
                BotToken = "bot_token_123",
                BotUsername = "elite_cuts_bot",
                TelegramId = "123456789",
                TimeZone = "Europe/Kyiv"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var ownerInDb = await context.BarbershopOwners.FirstOrDefaultAsync(o => o.Email == "newowner@test.com");
            Assert.NotNull(ownerInDb);
            Assert.Equal("John Doe", ownerInDb.OwnerName);
            Assert.Equal("+380991234567", ownerInDb.PhoneNumber);
            Assert.True(PasswordSecurity.VerifyPassword("Password123!", ownerInDb.PasswordHash));
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var existingOwner = TestDbContextHelper.CreateValidOwner(
                email: "existing@test.com",
                ownerName: "Existing Owner",
                phone: "+380991112233",
                password: "Pass123!"
            );
            context.BarbershopOwners.Add(existingOwner);
            await context.SaveChangesAsync();

            var controller = new RegestrationController(context, _configMock.Object, _emailServiceMock.Object);
            var request = new RegistrationRequest
            {
                Email = "EXISTING@test.com",
                OwnerName = "Another Owner",
                PhoneNumber = "+380993334455",
                Password = "Pass456!",
                BarbershopName = "Another Shop",
                BarbershopAddress = "Another Address",
                BotToken = "token",
                BotUsername = "user",
                TelegramId = "123"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Register_InvalidPhoneNumber_ReturnsBadRequest()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var controller = new RegestrationController(context, _configMock.Object, _emailServiceMock.Object);

            var request = new RegistrationRequest
            {
                Email = "phonecheck@test.com",
                OwnerName = "Owner",
                PhoneNumber = "12345", // No '+' prefix and too short
                Password = "Password123!",
                BarbershopName = "Shop",
                BarbershopAddress = "Address",
                BotToken = "token",
                BotUsername = "user",
                TelegramId = "123"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Login_ValidOwnerCredentials_ReturnsJwtTokenAndOwnerRole()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            string email = "loginowner@test.com";
            string password = "SecretPassword123!";
            var owner = TestDbContextHelper.CreateValidOwner(
                email: email,
                ownerName: "Owner User",
                phone: "+380991112233",
                password: password,
                status: OwnerStatus.Active
            );
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new RegestrationController(context, _configMock.Object, _emailServiceMock.Object);
            var loginRequest = new LoginRequest
            {
                Email = "LoginOwner@test.com",
                Password = password
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var roleProp = okResult.Value?.GetType().GetProperty("role")?.GetValue(okResult.Value)?.ToString();
            var tokenProp = okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value)?.ToString();

            Assert.Equal("Owner", roleProp);
            Assert.False(string.IsNullOrWhiteSpace(tokenProp));
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var owner = TestDbContextHelper.CreateValidOwner(
                email: "wrongpass@test.com",
                ownerName: "Owner",
                phone: "+380991112233",
                password: "CorrectPass123!"
            );
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new RegestrationController(context, _configMock.Object, _emailServiceMock.Object);
            var loginRequest = new LoginRequest
            {
                Email = "wrongpass@test.com",
                Password = "WrongPassPassword!"
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Login_ValidSaasAdmin_ReturnsAdminRole()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            string adminEmail = "admin@barbershop.b2b";
            string password = "Admin12345!";
            var (salt, hash) = PasswordSecurity.CreateHashAndSalt(password);
            var admin = new SaasAdmin
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                PasswordSalt = salt,
                PasswordHash = hash,
                CreatedAt = DateTime.UtcNow
            };
            context.SaasAdmins.Add(admin);
            await context.SaveChangesAsync();

            var controller = new RegestrationController(context, _configMock.Object, _emailServiceMock.Object);
            var loginRequest = new LoginRequest
            {
                Email = adminEmail,
                Password = password
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var roleProp = okResult.Value?.GetType().GetProperty("role")?.GetValue(okResult.Value)?.ToString();
            var tokenProp = okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value)?.ToString();

            Assert.Equal("Admin", roleProp);
            Assert.False(string.IsNullOrWhiteSpace(tokenProp));
        }

        [Fact]
        public async Task ForgotPassword_And_ResetPassword_FullFlow_Succeeds()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "resetme@test.com",
                ownerName: "Reset User",
                phone: "+380998887766",
                password: "OldPassword123!"
            );
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new RegestrationController(context, _configMock.Object, _emailServiceMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // 1. ForgotPassword
            var forgotResult = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "resetme@test.com" });
            Assert.IsType<OkObjectResult>(forgotResult);

            var updatedOwner = await context.BarbershopOwners.FindAsync(ownerId);
            Assert.NotNull(updatedOwner?.PasswordResetToken);
            Assert.True(updatedOwner!.PasswordResetTokenExpires > DateTime.UtcNow);

            string resetToken = updatedOwner.PasswordResetToken!;

            // 2. VerifyResetToken
            var verifyResult = await controller.VerifyResetToken(resetToken);
            var verifyOk = Assert.IsType<OkObjectResult>(verifyResult);
            var validProp = verifyOk.Value?.GetType().GetProperty("valid")?.GetValue(verifyOk.Value);
            Assert.Equal(true, validProp);

            // 3. ResetPassword with new password
            var resetResult = await controller.ResetPassword(new ResetPasswordRequest
            {
                Token = resetToken,
                NewPassword = "BrandNewPassword2026!"
            });
            Assert.IsType<OkObjectResult>(resetResult);

            var finalOwner = await context.BarbershopOwners.FindAsync(ownerId);
            Assert.Null(finalOwner!.PasswordResetToken);
            Assert.True(PasswordSecurity.VerifyPassword("BrandNewPassword2026!", finalOwner.PasswordHash));
            Assert.False(PasswordSecurity.VerifyPassword("OldPassword123!", finalOwner.PasswordHash));
        }

        [Fact]
        public async Task ChangePassword_AuthenticatedOwner_UpdatesPassword()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "changepass@test.com",
                ownerName: "Owner User",
                phone: "+380991112233",
                password: "CurrentPass123!"
            );
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new RegestrationController(context, _configMock.Object, _emailServiceMock.Object);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            // Act
            var result = await controller.ChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = "CurrentPass123!",
                NewPassword = "NewlyUpdatedPassword456!"
            });

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var refreshedOwner = await context.BarbershopOwners.FindAsync(ownerId);
            Assert.True(PasswordSecurity.VerifyPassword("NewlyUpdatedPassword456!", refreshedOwner!.PasswordHash));
        }
    }
}
