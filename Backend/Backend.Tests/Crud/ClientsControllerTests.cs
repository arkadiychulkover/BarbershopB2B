using Backend.Controllers;
using Backend.DTOs;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Crud
{
    public class ClientsControllerTests
    {
        private readonly BotService _botService;

        public ClientsControllerTests()
        {
            _botService = new BotService();
        }

        private class MockDbContextFactory : IDbContextFactory<Data.AppDbContext>
        {
            private readonly DbContextOptions<Data.AppDbContext> _options;
            public MockDbContextFactory(DbContextOptions<Data.AppDbContext> options)
            {
                _options = options;
            }

            public Data.AppDbContext CreateDbContext()
            {
                return new Data.AppDbContext(_options);
            }

            public Task<Data.AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new Data.AppDbContext(_options));
            }
        }

        [Fact]
        public async Task UpdatePhone_ValidPhoneNumber_UpdatesClientPhone()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var clientId = Guid.NewGuid();
            using (var seedContext = new Data.AppDbContext(options))
            {
                seedContext.Clients.Add(new Client
                {
                    Id = clientId,
                    Name = "Test Client",
                    Phone = "+380501111111",
                    TelegramId = "999"
                });
                await seedContext.SaveChangesAsync();
            }

            var factory = new MockDbContextFactory(options);
            var controller = new ClientsController(factory, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(clientId, "Client");

            var request = new UpdateClientPhoneRequest("+380509999999");

            // Act
            var result = await controller.UpdatePhone(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            using var verifyContext = new Data.AppDbContext(options);
            var clientInDb = await verifyContext.Clients.FindAsync(clientId);
            Assert.Equal("+380509999999", clientInDb!.Phone);
        }

        [Fact]
        public async Task UpdatePhone_InvalidPhoneFormat_ReturnsBadRequest()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var clientId = Guid.NewGuid();
            using (var seedContext = new Data.AppDbContext(options))
            {
                seedContext.Clients.Add(new Client { Id = clientId, Name = "Test Client", Phone = "+380501111111", TelegramId = "111" });
                await seedContext.SaveChangesAsync();
            }

            var factory = new MockDbContextFactory(options);
            var controller = new ClientsController(factory, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(clientId, "Client");

            // Act
            var result = await controller.UpdatePhone(new UpdateClientPhoneRequest("invalid-phone"));

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        }

        [Fact]
        public async Task CancelAppointment_ExistingScheduledAppointment_MarksAsCancelled()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var clientId = Guid.NewGuid();
            var apptId = Guid.NewGuid();
            var masterId = Guid.NewGuid();

            using (var seedContext = new Data.AppDbContext(options))
            {
                var owner = TestDbContextHelper.CreateValidOwner();
                var serviceName = new ServiceName { Id = Guid.NewGuid(), Name = "Classic Cut", OwnerId = owner.Id };
                var master = new Master { Id = masterId, Name = "Barber 1", Ip = System.Net.IPAddress.Loopback, OwnerId = owner.Id, Owner = owner };
                var service = new Service { Id = Guid.NewGuid(), MasterId = masterId, ServiceName = serviceName, ServiceNameId = serviceName.Id, Price = 300, Duration = 30 };
                var client = new Client { Id = clientId, Name = "Client 1", Phone = "+380501234567", TelegramId = "222", OwnerId = owner.Id };
                var appt = new Appointment
                {
                    Id = apptId,
                    ClientId = clientId,
                    MasterId = masterId,
                    ServiceId = service.Id,
                    Service = service,
                    Status = AppointmentStatus.Scheduled,
                    AppointmentDate = DateTime.UtcNow.AddDays(1)
                };

                seedContext.BarbershopOwners.Add(owner);
                seedContext.ServiceNames.Add(serviceName);
                seedContext.Masters.Add(master);
                seedContext.Clients.Add(client);
                seedContext.Services.Add(service);
                seedContext.Appointments.Add(appt);
                await seedContext.SaveChangesAsync();
            }

            var factory = new MockDbContextFactory(options);
            var controller = new ClientsController(factory, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(clientId, "Client");

            // Act
            var result = await controller.CancelAppointment(apptId);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            using var verifyContext = new Data.AppDbContext(options);
            var updatedAppt = await verifyContext.Appointments.FindAsync(apptId);
            Assert.Equal(AppointmentStatus.Cancelled, updatedAppt!.Status);
        }

        [Fact]
        public async Task Zapisatsa_ClientReachesMaxActiveBookingsLimit_ReturnsBadRequest()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var clientId = Guid.NewGuid();
            var masterId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();

            using (var seedContext = new Data.AppDbContext(options))
            {
                var owner = TestDbContextHelper.CreateValidOwner();
                owner.MaxActiveBookingsPerClient = 1;

                var master = new Master
                {
                    Id = masterId,
                    Name = "Master Test",
                    Ip = System.Net.IPAddress.Loopback,
                    OwnerId = owner.Id,
                    Owner = owner
                };

                var serviceName = new ServiceName { Id = Guid.NewGuid(), Name = "Haircut", OwnerId = owner.Id };
                var service = new Service
                {
                    Id = serviceId,
                    MasterId = masterId,
                    ServiceName = serviceName,
                    ServiceNameId = serviceName.Id,
                    Price = 500,
                    Duration = 30,
                    IsActive = true
                };

                var client = new Client
                {
                    Id = clientId,
                    Name = "Test Client",
                    Phone = "+380501234567",
                    TelegramId = "12345",
                    OwnerId = owner.Id
                };

                // Already has 1 scheduled future appointment
                var existingAppt = new Appointment
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    MasterId = masterId,
                    ServiceId = serviceId,
                    Status = AppointmentStatus.Scheduled,
                    AppointmentDate = DateTime.UtcNow.AddDays(2),
                    AppointmentEndDate = DateTime.UtcNow.AddDays(2).AddMinutes(30)
                };

                seedContext.BarbershopOwners.Add(owner);
                seedContext.Masters.Add(master);
                seedContext.ServiceNames.Add(serviceName);
                seedContext.Services.Add(service);
                seedContext.Clients.Add(client);
                seedContext.Appointments.Add(existingAppt);
                await seedContext.SaveChangesAsync();
            }

            var factory = new MockDbContextFactory(options);
            var controller = new ClientsController(factory, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(clientId, "Client");

            var request = new BookAppointmentRequest(
                MasterId: masterId,
                ServiceId: serviceId,
                AppointmentDate: DateTime.UtcNow.AddDays(3)
            );

            // Act
            var result = await controller.Zapisatsa(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("максимального лимита активных записей", badRequest.Value!.ToString()!);
        }
    }
}
