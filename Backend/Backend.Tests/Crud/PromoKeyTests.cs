using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Backend.Tests.Crud
{
    public class PromoKeyTests
    {
        [Fact]
        public void PromoKeyService_GeneratesValid128HexKey()
        {
            var key1 = PromoKeyService.GenerateRawKey();
            var key2 = PromoKeyService.GenerateRawKey();

            Assert.NotNull(key1);
            Assert.Equal(128, key1.Length);
            Assert.NotNull(key2);
            Assert.Equal(128, key2.Length);
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void PromoKeyService_HashAndVerify_WorksCorrectly()
        {
            var rawKey = PromoKeyService.GenerateRawKey();
            var (salt, keyHash) = PromoKeyService.HashKey(rawKey);

            Assert.False(string.IsNullOrWhiteSpace(salt));
            Assert.False(string.IsNullOrWhiteSpace(keyHash));

            // Проверка правильного ключа
            Assert.True(PromoKeyService.VerifyKey(rawKey, salt, keyHash));

            // Проверка с пробелами и в верхнем регистре
            Assert.True(PromoKeyService.VerifyKey("  " + rawKey.ToUpperInvariant() + "  ", salt, keyHash));

            // Проверка неправильного ключа
            Assert.False(PromoKeyService.VerifyKey("wrong-key", salt, keyHash));
        }

        [Fact]
        public async Task AdminController_CreatePromoKey_CreatesAndReturnsKey()
        {
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var controller = new AdminController(context, null!, null!);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(Guid.NewGuid(), role: "Admin");

            var request = new CreatePromoKeyRequest { Days = 45 };
            var result = await controller.CreatePromoKey(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdDto = Assert.IsType<CreatedPromoKeyResultDto>(okResult.Value);

            Assert.Equal(45, createdDto.Days);
            Assert.Equal(PromoKeyStatus.Created.ToString(), createdDto.Status);
            Assert.Equal(128, createdDto.RawKey.Length);

            var inDb = await context.PromoKeys.FindAsync(createdDto.Id);
            Assert.NotNull(inDb);
            Assert.Equal(PromoKeyStatus.Created, inDb.Status);
            Assert.True(PromoKeyService.VerifyKey(createdDto.RawKey, inDb.Salt, inDb.KeyHash));
        }

        [Fact]
        public async Task AdminController_UpdatePromoKey_UpdatesDaysWhenNotUsed()
        {
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var controller = new AdminController(context, null!, null!);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(Guid.NewGuid(), role: "Admin");

            var promoKey = new PromoKey
            {
                Id = Guid.NewGuid(),
                Days = 30,
                Salt = "salt",
                KeyHash = "hash",
                Status = PromoKeyStatus.Created,
                CreatedAt = DateTime.UtcNow
            };
            context.PromoKeys.Add(promoKey);
            await context.SaveChangesAsync();

            var updateResult = await controller.UpdatePromoKey(promoKey.Id, new UpdatePromoKeyRequest { Days = 60 });
            Assert.IsType<OkObjectResult>(updateResult);

            var updatedInDb = await context.PromoKeys.FindAsync(promoKey.Id);
            Assert.Equal(60, updatedInDb!.Days);
        }

        [Fact]
        public async Task AdminController_UpdatePromoKey_RejectsWhenUsedOrDeleted()
        {
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var controller = new AdminController(context, null!, null!);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(Guid.NewGuid(), role: "Admin");

            var usedKey = new PromoKey
            {
                Id = Guid.NewGuid(),
                Days = 30,
                Salt = "salt",
                KeyHash = "hash",
                Status = PromoKeyStatus.Used,
                CreatedAt = DateTime.UtcNow
            };
            context.PromoKeys.Add(usedKey);
            await context.SaveChangesAsync();

            var updateResult = await controller.UpdatePromoKey(usedKey.Id, new UpdatePromoKeyRequest { Days = 90 });
            Assert.IsType<BadRequestObjectResult>(updateResult);
        }

        [Fact]
        public async Task AdminController_DeletePromoKey_MarksAsDeleted()
        {
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var controller = new AdminController(context, null!, null!);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(Guid.NewGuid(), role: "Admin");

            var promoKey = new PromoKey
            {
                Id = Guid.NewGuid(),
                Days = 30,
                Salt = "salt",
                KeyHash = "hash",
                Status = PromoKeyStatus.Created,
                CreatedAt = DateTime.UtcNow
            };
            context.PromoKeys.Add(promoKey);
            await context.SaveChangesAsync();

            var deleteResult = await controller.DeletePromoKey(promoKey.Id);
            Assert.IsType<OkObjectResult>(deleteResult);

            var deletedInDb = await context.PromoKeys.FindAsync(promoKey.Id);
            Assert.Equal(PromoKeyStatus.Deleted, deletedInDb!.Status);
        }

        [Fact]
        public async Task PaymentController_RedeemPromoKey_ActivatesSubscription()
        {
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId, 
                status: OwnerStatus.Pending, 
                nextPayment: DateTime.UtcNow.AddDays(-5)
            );
            context.BarbershopOwners.Add(owner);

            var rawKey = PromoKeyService.GenerateRawKey();
            var (salt, keyHash) = PromoKeyService.HashKey(rawKey);
            var promoKey = new PromoKey
            {
                Id = Guid.NewGuid(),
                Days = 30,
                Salt = salt,
                KeyHash = keyHash,
                Status = PromoKeyStatus.Created,
                CreatedAt = DateTime.UtcNow
            };
            context.PromoKeys.Add(promoKey);
            await context.SaveChangesAsync();

            var configMock = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SubscriptionAmount", "10" }
                })
                .Build();

            var paymentController = new PaymentController(context, null!, configMock);
            paymentController.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, role: "Owner");

            var redeemResult = await paymentController.RedeemPromoKey(new RedeemPromoKeyRequest { Key = rawKey });
            var okResult = Assert.IsType<OkObjectResult>(redeemResult);

            var updatedOwner = await context.BarbershopOwners.FindAsync(ownerId);
            Assert.NotNull(updatedOwner);
            Assert.Equal(OwnerStatus.Active, updatedOwner.Status);
            Assert.False(updatedOwner.IsBlocked);
            Assert.True(updatedOwner.NextPayment > DateTime.UtcNow.AddDays(28));

            var updatedKey = await context.PromoKeys.FindAsync(promoKey.Id);
            Assert.NotNull(updatedKey);
            Assert.Equal(PromoKeyStatus.Used, updatedKey.Status);
            Assert.Equal(ownerId, updatedKey.UsedByOwnerId);
            Assert.NotNull(updatedKey.UsedAt);
        }

        [Fact]
        public async Task PaymentController_RedeemPromoKey_FailsOnSecondAttempt()
        {
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(id: ownerId);
            context.BarbershopOwners.Add(owner);

            var rawKey = PromoKeyService.GenerateRawKey();
            var (salt, keyHash) = PromoKeyService.HashKey(rawKey);
            var promoKey = new PromoKey
            {
                Id = Guid.NewGuid(),
                Days = 30,
                Salt = salt,
                KeyHash = keyHash,
                Status = PromoKeyStatus.Created,
                CreatedAt = DateTime.UtcNow
            };
            context.PromoKeys.Add(promoKey);
            await context.SaveChangesAsync();

            var configMock = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SubscriptionAmount", "10" }
                })
                .Build();

            var paymentController = new PaymentController(context, null!, configMock);
            paymentController.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, role: "Owner");

            // Первый ввод - успех
            var res1 = await paymentController.RedeemPromoKey(new RedeemPromoKeyRequest { Key = rawKey });
            Assert.IsType<OkObjectResult>(res1);

            // Второй ввод - ошибка
            var res2 = await paymentController.RedeemPromoKey(new RedeemPromoKeyRequest { Key = rawKey });
            var badRequest = Assert.IsType<BadRequestObjectResult>(res2);
            Assert.Contains("Недействительный или уже использованный", badRequest.Value!.ToString()!);
        }
    }
}
