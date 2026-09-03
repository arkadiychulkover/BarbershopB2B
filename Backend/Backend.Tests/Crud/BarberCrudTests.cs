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
    public class BarberCrudTests
    {
        private readonly BotService _botService;

        public BarberCrudTests()
        {
            _botService = new BotService();
        }

        [Fact]
        public async Task AddBarber_ActiveSubscription_CreatesMasterWithLoopbackIp()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "owner@test.com",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(30)
            );
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new BarberController(context, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            var request = new AddBarberRequest
            {
                Name = "Master Alex",
                Description = "Top stylist with 5 years experience",
                TelegramId = "11223344",
                TelegramUsername = "@alex_stylist"
            };

            // Act
            var result = await controller.AddBarber(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var valProp = okResult.Value?.GetType().GetProperty("barberId")?.GetValue(okResult.Value);
            Guid barberId = (Guid)valProp!;
            Assert.NotEqual(Guid.Empty, barberId);

            var createdMaster = await context.Masters.FindAsync(barberId);
            Assert.NotNull(createdMaster);
            Assert.Equal("Master Alex", createdMaster.Name);
            Assert.Equal("alex_stylist", createdMaster.TelegramUsername); // stripped '@'
            Assert.Equal(ownerId, createdMaster.OwnerId);
        }

        [Fact]
        public async Task AddBarber_ExpiredSubscription_ReturnsForbidden()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "owner@test.com",
                status: OwnerStatus.Frozen,
                nextPayment: DateTime.UtcNow.AddDays(-1)
            );
            context.BarbershopOwners.Add(owner);
            await context.SaveChangesAsync();

            var controller = new BarberController(context, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            var request = new AddBarberRequest
            {
                Name = "Master Max"
            };

            // Act
            var result = await controller.AddBarber(request);

            // Assert
            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
        }

        [Fact]
        public async Task UpdateBarber_BelongsToOwner_UpdatesDetails()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "owner@test.com",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(30)
            );
            var master = new Master
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "Old Master Name",
                Description = "Old desc",
                TelegramId = "100",
                Ip = System.Net.IPAddress.Loopback
            };
            context.BarbershopOwners.Add(owner);
            context.Masters.Add(master);
            await context.SaveChangesAsync();

            var controller = new BarberController(context, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            var updateRequest = new UpdateBarberRequest
            {
                BarberId = master.Id,
                Name = "New Master Name",
                Description = "New desc",
                TelegramId = "200",
                TelegramUsername = "@new_handle"
            };

            // Act
            var result = await controller.UpdateBarber(updateRequest);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var updatedInDb = await context.Masters.FindAsync(master.Id);
            Assert.Equal("New Master Name", updatedInDb!.Name);
            Assert.Equal("New desc", updatedInDb.Description);
            Assert.Equal("200", updatedInDb.TelegramId);
            Assert.Equal("new_handle", updatedInDb.TelegramUsername);
        }

        [Fact]
        public async Task UpdateBarber_AnotherOwnerMaster_ReturnsNotFoundIdorProtection()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var owner1Id = Guid.NewGuid();
            var owner2Id = Guid.NewGuid();

            var owner1 = TestDbContextHelper.CreateValidOwner(id: owner1Id, email: "o1@test.com", status: OwnerStatus.Active, nextPayment: DateTime.UtcNow.AddDays(10));
            var owner2 = TestDbContextHelper.CreateValidOwner(id: owner2Id, email: "o2@test.com", status: OwnerStatus.Active, nextPayment: DateTime.UtcNow.AddDays(10));
            var master = new Master { Id = Guid.NewGuid(), OwnerId = owner2Id, Name = "Owner2 Master", Ip = System.Net.IPAddress.Loopback };

            context.BarbershopOwners.AddRange(owner1, owner2);
            context.Masters.Add(master);
            await context.SaveChangesAsync();

            var controller = new BarberController(context, _botService);
            // Owner 1 attempts to update Owner 2's master
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(owner1Id, "Owner");

            var updateRequest = new UpdateBarberRequest
            {
                BarberId = master.Id,
                Name = "Hacked Name"
            };

            // Act
            var result = await controller.UpdateBarber(updateRequest);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteBarber_CascadeCleansRelatedShiftsAndServices()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "owner@test.com",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(30)
            );
            var master = new Master { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Barber To Delete", Ip = System.Net.IPAddress.Loopback };
            var shift = new Shift { Id = Guid.NewGuid(), MasterId = master.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(18, 0) };
            var serviceName = new ServiceName { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Haircut" };
            var service = new Service { Id = Guid.NewGuid(), MasterId = master.Id, ServiceNameId = serviceName.Id, Price = 500, Duration = 45 };

            context.BarbershopOwners.Add(owner);
            context.Masters.Add(master);
            context.Shifts.Add(shift);
            context.ServiceNames.Add(serviceName);
            context.Services.Add(service);
            await context.SaveChangesAsync();

            var controller = new BarberController(context, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(ownerId, "Owner");

            // Act
            var result = await controller.DeleteBarber(new DeleteBarberRequest { BarberId = master.Id });

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Null(await context.Masters.FindAsync(master.Id));
            Assert.Empty(await context.Shifts.Where(s => s.MasterId == master.Id).ToListAsync());
            Assert.Empty(await context.Services.Where(s => s.MasterId == master.Id).ToListAsync());
        }

        [Fact]
        public async Task UpdateMyAppointment_ExistingTelegramClient_PreservesClientAndUpdatesPhone()
        {
            // Arrange
            using var context = TestDbContextHelper.CreateInMemoryDbContext();
            var ownerId = Guid.NewGuid();
            var owner = TestDbContextHelper.CreateValidOwner(
                id: ownerId,
                email: "owner@test.com",
                status: OwnerStatus.Active,
                nextPayment: DateTime.UtcNow.AddDays(30)
            );
            var master = new Master { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Master John", Ip = System.Net.IPAddress.Loopback };
            var serviceName = new ServiceName { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Стрижка" };
            var service = new Service { Id = Guid.NewGuid(), MasterId = master.Id, ServiceNameId = serviceName.Id, Price = 500, Duration = 60, IsActive = true };
            var client = new Client
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "Graff",
                TelegramId = "998877",
                TelegramUsername = "Eyed_graff",
                Phone = null
            };
            var apptDate = DateTime.UtcNow.AddDays(1);
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                MasterId = master.Id,
                ClientId = client.Id,
                ServiceId = service.Id,
                AppointmentDate = apptDate,
                AppointmentEndDate = apptDate.AddMinutes(60),
                Status = AppointmentStatus.Scheduled
            };

            context.BarbershopOwners.Add(owner);
            context.Masters.Add(master);
            context.ServiceNames.Add(serviceName);
            context.Services.Add(service);
            context.Clients.Add(client);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();

            var controller = new BarberController(context, _botService);
            controller.ControllerContext = TestDbContextHelper.CreateControllerContextWithUser(master.Id, "Master");

            var request = new BarberController.BarberAppointmentRequest
            {
                ClientId = client.Id,
                ServiceId = service.Id,
                AppointmentDate = apptDate,
                ClientName = "Graff",
                ClientPhone = "+380501234567",
                Status = AppointmentStatus.Scheduled
            };

            // Act
            var result = await controller.UpdateMyAppointment(appointment.Id, request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var updatedAppt = await context.Appointments.Include(a => a.Client).FirstOrDefaultAsync(a => a.Id == appointment.Id);
            Assert.NotNull(updatedAppt);
            Assert.Equal(client.Id, updatedAppt.ClientId);
            Assert.Equal("Graff", updatedAppt.Client.Name);
            Assert.Equal("998877", updatedAppt.Client.TelegramId);
            Assert.Equal("Eyed_graff", updatedAppt.Client.TelegramUsername);
            Assert.Equal("+380501234567", updatedAppt.Client.Phone);

            // Also check GetMasterAppointments returns ClientPhone
            var getResult = await controller.GetMasterAppointments();
            var okGetResult = Assert.IsType<OkObjectResult>(getResult);
            var dtoList = Assert.IsAssignableFrom<IEnumerable<AppointmentDto>>(okGetResult.Value);
            var apptDto = dtoList.FirstOrDefault(a => a.Id == appointment.Id);
            Assert.NotNull(apptDto);
            Assert.Equal("+380501234567", apptDto.ClientPhone);
            Assert.Equal("Eyed_graff", apptDto.ClientTelegramUsername);
        }
    }
}
