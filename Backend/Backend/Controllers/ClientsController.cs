using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Authorize(Roles = "Client")]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly BotService _botService;

        public ClientsController(IDbContextFactory<AppDbContext> dbContextFactory, BotService botService)
        {
            _dbContextFactory = dbContextFactory;
            _botService = botService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var clientId = User.GetUserId();

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var client = await context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId);
            if (client == null) return NotFound("Client not found.");

            return Ok(new ClientProfileDto
            {
                Id = client.Id,
                Name = client.Name,
                Phone = client.Phone,
                TelegramId = client.TelegramId
            });
        }

        [HttpPost("update-phone")]
        public async Task<IActionResult> UpdatePhone([FromBody] UpdateClientPhoneRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Phone))
                return BadRequest("Номер телефона не может быть пустым.");

            var clientId = User.GetUserId();

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var client = await context.Clients.FindAsync(clientId);
            if (client == null) return NotFound("Client not found.");

            client.Phone = request.Phone.Trim();
            await context.SaveChangesAsync();

            return Ok(new { Message = "Phone updated successfully", Phone = client.Phone });
        }

        [HttpPost("Zapisatsa")]
        public async Task<IActionResult> Zapisatsa([FromBody] BookAppointmentRequest request)
        {
            var clientId = User.GetUserId();

            using var context = await _dbContextFactory.CreateDbContextAsync();

            var client = await context.Clients.FindAsync(clientId);
            if (client == null)
                return NotFound("Client not found.");

            if (string.IsNullOrWhiteSpace(client.Phone))
                return BadRequest("Для записи необходимо указать номер телефона.");

            var master = await context.Masters
                .Include(m => m.Owner)
                .FirstOrDefaultAsync(m => m.Id == request.MasterId);
            if (master == null)
                return NotFound("Master not found.");

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Запись невозможна: подписка заведения не активна или истекла." });

            var service = await context.Services
                .Include(s => s.ServiceName)
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.MasterId == request.MasterId);
            if (service == null || !service.IsActive)
                return NotFound("Service not found or is inactive for this master.");

            var dayOfWeek = request.AppointmentDate.DayOfWeek;
            var shift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.MasterId == request.MasterId && s.DayOfWeek == dayOfWeek);
            if (shift == null)
                return BadRequest("The master does not have a shift on this date.");

            var appointmentStartTime = TimeOnly.FromDateTime(request.AppointmentDate);
            var appointmentEndTime = appointmentStartTime.AddMinutes(service.Duration);

            if (appointmentStartTime < shift.StartTime || appointmentEndTime > shift.EndTime)
                return BadRequest("The requested time is outside the master's working hours.");

            if (request.AppointmentDate < DateTime.UtcNow)
                return BadRequest("Cannot book an appointment in the past.");

            var appointmentEndDateTime = request.AppointmentDate.AddMinutes(service.Duration);

            // ACID transaction check to prevent race conditions / double bookings
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                bool isOverlap = await context.Appointments.AnyAsync(a =>
                    a.MasterId == request.MasterId
                    && a.Status != AppointmentStatus.Cancelled
                    && request.AppointmentDate < a.AppointmentEndDate
                    && appointmentEndDateTime > a.AppointmentDate);

                if (isOverlap)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("The requested time overlaps with another appointment.");
                }

                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    MasterId = request.MasterId,
                    ClientId = clientId,
                    ServiceId = request.ServiceId,
                    AppointmentDate = request.AppointmentDate,
                    AppointmentEndDate = appointmentEndDateTime,
                    Status = AppointmentStatus.Scheduled,
                    MasterProfit = 0,
                    OwnerProfit = 0,
                    DepositPaid = false
                };

                context.Appointments.Add(appointment);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (!string.IsNullOrEmpty(master.TelegramId) && !string.IsNullOrEmpty(master.Owner?.BotToken))
                {
                    try
                    {
                        var timeStr = request.AppointmentDate.ToString("HH:mm");
                        var clientName = client.Name ?? "Клиент";
                        var clientPhoneStr = !string.IsNullOrWhiteSpace(client.Phone) ? $" ({client.Phone})" : "";
                        var message = $"Новая запись!\nК вам записался {clientName}{clientPhoneStr} на услугу «{service.ServiceName?.Name}».\nВремя: {timeStr}.";

                        if (long.TryParse(master.TelegramId, out long chatId))
                            await _botService.SendMessageAsync(master.Owner.BotToken, chatId, message);
                    }
                    catch { }
                }

                return Ok(new { Message = "Appointment booked successfully", AppointmentId = appointment.Id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpGet("Masters")]
        public async Task<IActionResult> GetMasters()
        {
            var clientId = User.GetUserId();

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var client = await context.Clients.Include(c => c.Owner).AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId);
            if (client == null) return NotFound("Client not found.");

            if (client.Owner == null || !client.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var masters = await context.Masters
                .AsNoTracking()
                .Where(m => m.OwnerId == client.OwnerId && m.IsActive)
                .Select(m => new ClientMasterDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    PhotoUrl = m.PhotoUrl,
                    Description = m.Description
                })
                .ToListAsync();

            return Ok(masters);
        }

        [HttpGet("Services/{masterId}")]
        public async Task<IActionResult> GetServices(Guid masterId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var master = await context.Masters.Include(m => m.Owner).AsNoTracking().FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null) return NotFound("Master not found.");

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var services = await context.Services
                .AsNoTracking()
                .Include(s => s.ServiceName)
                .Where(s => s.MasterId == masterId && s.IsActive)
                .Select(s => new ClientServiceDto
                {
                    Id = s.Id,
                    Name = s.ServiceName.Name,
                    Price = s.Price,
                    Duration = s.Duration,
                    Description = s.Description
                })
                .ToListAsync();

            return Ok(services);
        }

        [HttpGet("AvailableTimeSlots")]
        public async Task<IActionResult> GetAvailableTimeSlots([FromQuery] Guid masterId, [FromQuery] Guid serviceId, [FromQuery] DateTime date)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var service = await context.Services
                .AsNoTracking()
                .Include(s => s.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(s => s.Id == serviceId && s.MasterId == masterId);
            if (service == null || !service.IsActive) return NotFound("Service not found.");

            if (service.Master?.Owner == null || !service.Master.Owner.HasActiveSubscription())
                return Ok(new List<string>());

            var dayOfWeek = date.DayOfWeek;
            var shift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.MasterId == masterId && s.DayOfWeek == dayOfWeek);
            if (shift == null) return Ok(new List<string>());

            var startOfDay = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            var appointments = await context.Appointments
                .AsNoTracking()
                .Where(a => a.MasterId == masterId
                            && a.AppointmentDate >= startOfDay
                            && a.AppointmentDate < endOfDay
                            && a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();

            var availableSlots = new List<string>();
            var currentTime = shift.StartTime;

            while (currentTime.AddMinutes(service.Duration) <= shift.EndTime)
            {
                var slotEnd = currentTime.AddMinutes(service.Duration);
                var slotStartDateTime = startOfDay.Add(currentTime.ToTimeSpan());
                var slotEndDateTime = startOfDay.Add(slotEnd.ToTimeSpan());

                bool isOverlap = appointments.Any(a =>
                    slotStartDateTime < a.AppointmentEndDate && slotEndDateTime > a.AppointmentDate);

                if (!isOverlap && slotStartDateTime > DateTime.UtcNow)
                    availableSlots.Add(currentTime.ToString("HH:mm"));

                currentTime = currentTime.AddMinutes(30);
            }

            return Ok(availableSlots);
        }

        [HttpGet("get-my-appointments")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var clientId = User.GetUserId();

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var appointments = await context.Appointments
                .AsNoTracking()
                .Where(a => a.ClientId == clientId)
                .Include(a => a.Master)
                .Include(a => a.Service)
                .Select(a => new ClientAppointmentDto
                {
                    Id = a.Id,
                    MasterId = a.MasterId,
                    MasterName = a.Master.Name,
                    ServiceName = a.Service.ServiceName.Name,
                    AppointmentDate = a.AppointmentDate,
                    AppointmentEndDate = a.AppointmentEndDate,
                    Status = a.Status,
                    Price = a.Service.Price,
                    HasReview = a.ReviewId != Guid.Empty
                })
                .ToListAsync();

            return Ok(appointments);
        }

        [HttpDelete("cancel-appointment")]
        public async Task<IActionResult> CancelAppointment([FromQuery] Guid appointmentId)
        {
            var clientId = User.GetUserId();

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var appointment = await context.Appointments
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .Include(a => a.Service)
                    .ThenInclude(s => s.ServiceName)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == clientId);

            if (appointment == null)
                return NotFound("Appointment not found.");
            if (appointment.Status == AppointmentStatus.Cancelled)
                return BadRequest("Appointment is already cancelled.");

            appointment.Status = AppointmentStatus.Cancelled;
            await context.SaveChangesAsync();

            if (appointment.Master?.Owner != null && appointment.Master.Owner.HasActiveSubscription() && !string.IsNullOrEmpty(appointment.Master.Owner.BotToken) && !string.IsNullOrEmpty(appointment.Master.TelegramId))
            {
                try
                {
                    await _botService.SendMessageAsync(
                        appointment.Master.Owner.BotToken,
                        long.Parse(appointment.Master.TelegramId),
                        $"Клиент отменил запись на услугу «{appointment.Service.ServiceName.Name}» в {appointment.AppointmentDate:HH:mm}.");
                }
                catch { }
            }

            return Ok(new { Message = "Appointment cancelled successfully" });
        }

        [HttpPost("submit-review")]
        public async Task<IActionResult> PostReview([FromBody] ReviewDTO review)
        {
            var clientId = User.GetUserId();

            using var context = await _dbContextFactory.CreateDbContextAsync();

            var appointment = await context.Appointments
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(a => a.Id == review.AppointmentId && a.ClientId == clientId);
            var master = await context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == review.BarberId);

            if (appointment == null) return NotFound("Appointment not found.");
            if (master == null) return NotFound("Master not found.");

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            if (appointment.Status != AppointmentStatus.Completed)
                return BadRequest("Cannot review an appointment that is not completed.");

            if (appointment.ReviewId != Guid.Empty || await context.Reviews.AnyAsync(r => r.AppointmentId == review.AppointmentId))
                return BadRequest("A review has already been submitted for this appointment.");

            var newReview = new Review
            {
                Id = Guid.NewGuid(),
                MasterId = review.BarberId,
                ClientId = clientId,
                AppointmentId = review.AppointmentId,
                Rating = review.Rating,
                Comment = review.Comment
            };

            context.Reviews.Add(newReview);
            appointment.ReviewId = newReview.Id;

            int reviewsCount = await context.Reviews.CountAsync(r => r.MasterId == review.BarberId);
            decimal totalRating = await context.Reviews
                .Where(r => r.MasterId == review.BarberId)
                .SumAsync(r => r.Rating);
            decimal newAverageRating = (totalRating + review.Rating) / (reviewsCount + 1);

            master.Rating = newAverageRating;
            await context.SaveChangesAsync();

            return Ok(new { Message = "Review submitted successfully" });
        }

        [HttpGet("get-reviews")]
        public async Task<IActionResult> GetReviews([FromQuery] Guid barberId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var reviews = await context.Reviews
                .AsNoTracking()
                .Where(r => r.MasterId == barberId)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    ClientName = r.Client.Name,
                    AppointmentDate = r.Appointment.AppointmentDate
                })
                .ToListAsync();
            return Ok(reviews);
        }

        [HttpGet("get-master-rating")]
        public async Task<IActionResult> GetMasterRating([FromQuery] Guid barberId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var master = await context.Masters.AsNoTracking().FirstOrDefaultAsync(m => m.Id == barberId);
            if (master == null) return NotFound("Master not found.");
            return Ok(new { Rating = master.Rating });
        }
    }

    internal class ClientAppointmentDto
    {
        public Guid Id { get; set; }
        public Guid MasterId { get; set; }
        public string MasterName { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentEndDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public decimal? Price { get; set; }
        public bool HasReview { get; set; }
    }

    public class ReviewDTO
    {
        public Guid BarberId { get; set; }
        public Guid ClientId { get; set; }
        public Guid AppointmentId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}
