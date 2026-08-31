using Backend.Controllers;
using Backend.DTOs;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Crud
{
    public class SettingsCrudTests
    {
        [Fact]
        public async Task GetSettings_ValidOwner_ReturnsOwnerSettings()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "settingsowner@test.com",
                ownerName: "George",
                phone: "+380501112233",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(25)
            );
            owner.BarbershopName = "Old Town Barbershop";
            owner.BarbershopAddress = "Central Ave 5";
            owner.WalletAddress = "EQD123456789TONWalletAddressTest";

            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new SettingsController(context);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            // Act
            var result = await controller.GetSettings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SettingsResponse>(okResult.Value);
            Assert.Equal("Old Town Barbershop", response.BarbershopName);
            Assert.Equal("Central Ave 5", response.BarbershopAddress);
            Assert.Equal("+380501112233", response.PhoneNumber);
            Assert.True(response.IsSubscribed);
        }

        [Fact]
        public async Task UpdateSettings_ActiveSub_UpdatesAllFields()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "settingsowner@test.com",
                ownerName: "George",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(25)
            );
            owner.BarbershopName = "Initial Barbershop";

            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new SettingsController(context);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            var updateRequest = new UpdateSettingsRequest
            {
                BarbershopName = "Modern Scissors",
                BarbershopAddress = "Updated Address 12",
                OwnerName = "George Lucas",
                PhoneNumber = "+380509998877",
                TimeZone = "Europe/Warsaw",
                ReminderHoursBefore = 3,
                WinBackDays = 45,
                MasterFee = 15.5m,
                WalletAddress = "EQNewTonWalletAddress1234567"
            };

            // Act
            var result = await controller.UpdateSettings(updateRequest);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var updatedOwner = await context.BarbershopOwners.FindAsync(ownerId);
            Assert.Equal("Modern Scissors", updatedOwner!.BarbershopName);
            Assert.Equal("Updated Address 12", updatedOwner.BarbershopAddress);
            Assert.Equal("George Lucas", updatedOwner.OwnerName);
            Assert.Equal("+380509998877", updatedOwner.PhoneNumber);
            Assert.Equal("Europe/Warsaw", updatedOwner.TimeZone);
            Assert.Equal(3, updatedOwner.ReminderHoursBefore);
            Assert.Equal(45, updatedOwner.WinBackDays);
            Assert.Equal(15.5m, updatedOwner.MasterFee);
            Assert.Equal("EQNewTonWalletAddress1234567", updatedOwner.WalletAddress);
        }

        [Fact]
        public async Task UpdateSettings_InactiveSubscription_OnlyAllowsWalletUpdate()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "frozenowner@test.com",
                status: OwnerStatus.Frozen,
                nextPayment: DateTime.UtcNow.AddDays(-2)
            );
            owner.BarbershopName = "Unchanged Name";
            owner.WalletAddress = "OldWallet";
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new SettingsController(context);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            // Attempting to change name while inactive
            var invalidRequest = new UpdateSettingsRequest
            {
                BarbershopName = "Attempted Name Change",
                WalletAddress = "NewWallet123"
            };

            // Act
            var result = await controller.UpdateSettings(invalidRequest);

            // Assert: wallet is saved even without active sub so owner can pay
            Assert.IsType<OkObjectResult>(result);

            var updatedOwner = await context.BarbershopOwners.FindAsync(ownerId);
            Assert.Equal("NewWallet123", updatedOwner!.WalletAddress);
            // BarbershopName must remain unchanged because subscription is frozen
            Assert.Equal("Unchanged Name", updatedOwner.BarbershopName);
        }
    }
}
