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
    public class ServiceNamesCrudTests
    {
        [Fact]
        public async Task GetServiceNames_ReturnsOnlyServicesForCurrentOwner()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var owner1Id = Guid.NewGuid();
            var owner2Id = Guid.NewGuid();

            var owner1 = TestDbContextHelper.CreateValidOwner(id: owner1Id, email: "o1@test.com", status: OwnerStatus.Active, nextPayment: DateTime.UtcNow.AddDays(30));
            var owner2 = TestDbContextHelper.CreateValidOwner(id: owner2Id, email: "o2@test.com", status: OwnerStatus.Active, nextPayment: DateTime.UtcNow.AddDays(30));
            context.BarbershopOwners.AddRange(owner1, owner2);

            context.ServiceNames.AddRange(
                new ServiceName { Id = Guid.NewGuid(), Name = "Men's Haircut", OwnerId = owner1Id },
                new ServiceName { Id = Guid.NewGuid(), Name = "Beard Trim", OwnerId = owner1Id },
                new ServiceName { Id = Guid.NewGuid(), Name = "Foreign Service", OwnerId = owner2Id }
            );
            await context.SaveChangesAsync();

            var controller = new ServiceNamesController(context);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(owner1Id, "Owner");

            // Act
            var result = await controller.GetServiceNames();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsAssignableFrom<IEnumerable<ServiceNameDto>>(okResult.Value);
            var list = items.ToList();
            Assert.Equal(2, list.Count);
            Assert.Contains(list, s => s.Name == "Men's Haircut");
            Assert.Contains(list, s => s.Name == "Beard Trim");
            Assert.DoesNotContain(list, s => s.Name == "Foreign Service");
        }

        [Fact]
        public async Task CreateServiceName_ActiveSubscription_CreatesSuccessfully()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "active@test.com",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(15)
            );
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new ServiceNamesController(context);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            var request = new CreateServiceNameRequest { Name = "Royal Shave" };

            // Act
            var result = await controller.CreateServiceName(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var createdDto = Assert.IsType<ServiceNameDto>(createdResult.Value);
            Assert.Equal("Royal Shave", createdDto.Name);

            var inDb = await context.ServiceNames.FirstOrDefaultAsync(s => s.Id == createdDto.Id);
            Assert.NotNull(inDb);
            Assert.Equal(ownerId, inDb.OwnerId);
        }

        [Fact]
        public async Task CreateServiceName_ExpiredSubscription_ReturnsForbidden()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "expired@test.com",
                status: OwnerStatus.Frozen,
                nextPayment: DateTime.UtcNow.AddDays(-5)
            );
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new ServiceNamesController(context);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            var request = new CreateServiceNameRequest { Name = "Clipper Cut" };

            // Act
            var result = await controller.CreateServiceName(request);

            // Assert
            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
        }

        [Fact]
        public async Task UpdateServiceName_ValidOwnerAndActiveSub_UpdatesName()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "active@test.com",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(20)
            );
            var service = new ServiceName { Id = Guid.NewGuid(), Name = "Old Name", OwnerId = ownerId };
            context.BarbershopOwners.Add(owner);
            context.ServiceNames.Add(service);
            await context.SaveChangesAsync();

            var controller = new ServiceNamesController(context);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            var request = new UpdateServiceNameRequest { Name = "Updated Name" };

            // Act
            var result = await controller.UpdateServiceName(service.Id, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedDto = Assert.IsType<ServiceNameDto>(okResult.Value);
            Assert.Equal("Updated Name", updatedDto.Name);

            var dbService = await context.ServiceNames.FindAsync(service.Id);
            Assert.Equal("Updated Name", dbService!.Name);
        }

        [Fact]
        public async Task DeleteServiceName_ValidOwner_RemovesService()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "active@test.com",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(20)
            );
            var service = new ServiceName { Id = Guid.NewGuid(), Name = "Service To Delete", OwnerId = ownerId };
            context.BarbershopOwners.Add(owner);
            context.ServiceNames.Add(service);
            await context.SaveChangesAsync();

            var controller = new ServiceNamesController(context);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            // Act
            var result = await controller.DeleteServiceName(service.Id);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var dbService = await context.ServiceNames.FindAsync(service.Id);
            Assert.Null(dbService);
        }

        [Fact]
        public async Task DeleteServiceName_WithRelatedServicesAndAppointments_CascadesDeletion()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "owner_cascade@test.com",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(30)
            );
            owner.BotToken = "fake-token";
            context.BarbershopOwners.Add(owner);

            var serviceName = new ServiceName { Id = Guid.NewGuid(), Name = "Full Beard", OwnerId = ownerId };
            context.ServiceNames.Add(serviceName);

            var master = new Master
            {
                Id = Guid.NewGuid(),
                Name = "John",
                OwnerId = ownerId,
                Ip = System.Net.IPAddress.Loopback
            };
            context.Masters.Add(master);

            var masterService = new Service
            {
                Id = Guid.NewGuid(),
                ServiceNameId = serviceName.Id,
                MasterId = master.Id,
                Price = 1500,
                Duration = 45
            };
            context.Services.Add(masterService);

            var client = new Client
            {
                Id = Guid.NewGuid(),
                Name = "Client 1",
                OwnerId = ownerId,
                TelegramId = "12345678"
            };
            context.Clients.Add(client);

            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                MasterId = master.Id,
                ClientId = client.Id,
                ServiceId = masterService.Id,
                AppointmentDate = DateTime.UtcNow.AddHours(3),
                AppointmentEndDate = DateTime.UtcNow.AddHours(4),
                Status = AppointmentStatus.Scheduled
            };
            context.Appointments.Add(appt);
            await context.SaveChangesAsync();

            var controller = new ServiceNamesController(context, new Backend.Services.BotService());
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            // Act
            var result = await controller.DeleteServiceName(serviceName.Id);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Null(await context.ServiceNames.FindAsync(serviceName.Id));
            Assert.Null(await context.Services.FindAsync(masterService.Id));
            Assert.Null(await context.Appointments.FindAsync(appt.Id));
        }
    }
}
