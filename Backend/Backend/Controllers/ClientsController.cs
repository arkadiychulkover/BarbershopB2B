using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
                return BadRequest(new { message = "Номер телефона не может быть пустым." });

            var cleanedPhone = Regex.Replace(request.Phone.Trim(), @"[\s\-\(\)]", "");

            var phoneRegex = new Regex(@"^\+[0-9]{1,3}[0-9]{9}$");
            if (!phoneRegex.IsMatch(cleanedPhone))
            {
                return BadRequest(new { message = "Некорректный номер телефона. Номер должен начинаться с \"+\", содержать код страны (1-3 цифры) и 9 цифр номера (например, +380991234567 или +79991234567)." });
            }

            var clientId = User.GetUserId();

            using var context = await _dbContextFactory.CreateDbContextAsync();
            var client = await context.Clients.FindAsync(clientId);
            if (client == null) return NotFound(new { message = "Клиент не найден." });

            client.Phone = cleanedPhone;
            await context.SaveChangesAsync();

            return Ok(new { message = "Номер телефона успешно сохранен", phone = client.Phone });
        }

        private static TimeZoneInfo GetSalonTimeZone(string? tz)
        {
            if (string.IsNullOrWhiteSpace(tz)) tz = "Europe/Kyiv";
            try { return TimeZoneInfo.FindSystemTimeZoneById(tz); }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(tz switch
                    {
                        "Europe/Kyiv" or "Europe/Kiev" => "FLE Standard Time",
                        "Europe/Moscow" => "Russian Standard Time",
                        _ => "UTC"
                    });
                }
                catch { return TimeZoneInfo.Utc; }
            }
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

            // IDOR защита: клиент не может записаться к мастеру другого заведения
            if (client.OwnerId != master.OwnerId)
                return BadRequest(new { message = "Мастер не принадлежит вашему заведению." });

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Запись невозможна: подписка заведения не активна или истекла." });

            // Проверка лимита активных записей для одного клиента
            if (master.Owner.MaxActiveBookingsPerClient > 0)
            {
                var nowUtc = DateTime.UtcNow;
                var activeAppointmentsCount = await context.Appointments.CountAsync(a =>
                    a.ClientId == clientId &&
                    a.Status == AppointmentStatus.Scheduled &&
                    a.AppointmentEndDate > nowUtc);

                if (activeAppointmentsCount >= master.Owner.MaxActiveBookingsPerClient)
                {
                    return BadRequest(new 
                    { 
                        message = $"Вы достигли максимального лимита активных записей ({master.Owner.MaxActiveBookingsPerClient}). Дождитесь визита или отмените предыдущую запись, чтобы выбрать другое время." 
                    });
                }
            }

            // Проверка отпуска мастера
            var bookingDateUtc = DateTime.SpecifyKind(request.AppointmentDate.Date, DateTimeKind.Utc);
            bool onVacation = await context.MasterVacations.AnyAsync(v =>
                v.MasterId == master.Id &&
                v.StartDate <= bookingDateUtc &&
                v.EndDate >= bookingDateUtc);
            if (onVacation)
                return BadRequest(new { message = "Мастер недоступен в эту дату (отпуск или больничный)." });

            // Загружаем основную услугу
            var service = await context.Services
                .Include(s => s.ServiceName)
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.MasterId == request.MasterId);
            if (service == null || !service.IsActive)
                return NotFound("Service not found or is inactive for this master.");

            // Загружаем дополнительные услуги
            var additionalServices = new List<Models.Service>();
            if (request.AdditionalServiceIds != null && request.AdditionalServiceIds.Count > 0)
            {
                foreach (var addSvcId in request.AdditionalServiceIds.Distinct())
                {
                    if (addSvcId == request.ServiceId) continue; // не дублируем основную
                    var addSvc = await context.Services
                        .Include(s => s.ServiceName)
                        .FirstOrDefaultAsync(s => s.Id == addSvcId && s.MasterId == request.MasterId && s.IsActive);
                    if (addSvc == null)
                        return NotFound($"Additional service {addSvcId} not found or inactive.");
                    additionalServices.Add(addSvc);
                }
            }

            // Суммарная длительность
            int totalDuration = service.Duration + additionalServices.Sum(s => s.Duration);

            var dayOfWeek = request.AppointmentDate.DayOfWeek;
            var shift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.MasterId == request.MasterId && s.DayOfWeek == dayOfWeek);
            if (shift == null)
                return BadRequest("The master does not have a shift on this date.");

            var appointmentStartTime = TimeOnly.FromDateTime(request.AppointmentDate);
            var appointmentEndTime = appointmentStartTime.AddMinutes(totalDuration);

            if (appointmentStartTime < shift.StartTime || appointmentEndTime > shift.EndTime)
                return BadRequest("The requested time is outside the master's working hours.");

            // Проверка перерыва
            if (shift.BreakStartTime.HasValue && shift.BreakEndTime.HasValue)
            {
                bool overlapsBreak = appointmentStartTime < shift.BreakEndTime.Value && appointmentEndTime > shift.BreakStartTime.Value;
                if (overlapsBreak)
                    return BadRequest(new { message = $"Это время попадает в перерыв мастера ({shift.BreakStartTime.Value:HH\\:mm} – {shift.BreakEndTime.Value:HH\\:mm})." });
            }

            // Проверка на прошедшее время в часовом поясе заведения
            var salonTimeZone = GetSalonTimeZone(master.Owner?.TimeZone);
            var nowInSalon = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, salonTimeZone);
            var apptDateOnly = request.AppointmentDate.Date;
            var apptTimeOnly = TimeOnly.FromDateTime(request.AppointmentDate);
            if (apptDateOnly < nowInSalon.Date || (apptDateOnly == nowInSalon.Date && apptTimeOnly <= TimeOnly.FromDateTime(nowInSalon)))
                return BadRequest(new { message = "Нельзя записаться на прошедшее время." });

            var appointmentEndDateTime = request.AppointmentDate.AddMinutes(totalDuration);
            int breakMinutes = shift.BreakDurationMinutes;
            var appointmentEndWithBuffer = appointmentEndDateTime.AddMinutes(breakMinutes);
            var requestDateMinusBuffer = request.AppointmentDate.AddMinutes(-breakMinutes);

            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                bool isOverlap = await context.Appointments.AnyAsync(a =>
                    a.MasterId == request.MasterId
                    && a.Status != AppointmentStatus.Cancelled
                    && requestDateMinusBuffer < a.AppointmentEndDate
                    && appointmentEndWithBuffer > a.AppointmentDate);

                if (isOverlap)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("The requested time overlaps with another appointment or buffer time.");
                }

                DateTime? reminderTime = request.ReminderTime;
                if (!reminderTime.HasValue && request.ReminderHoursBefore.HasValue)
                    reminderTime = request.AppointmentDate.Subtract(TimeSpan.FromHours(request.ReminderHoursBefore.Value));
                else if (!reminderTime.HasValue)
                    reminderTime = request.AppointmentDate.Subtract(TimeSpan.FromHours(master.Owner?.ReminderHoursBefore ?? 2));

                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    MasterId = request.MasterId,
                    ClientId = clientId,
                    ServiceId = request.ServiceId,
                    AppointmentDate = DateTime.SpecifyKind(request.AppointmentDate, DateTimeKind.Utc),
                    AppointmentEndDate = DateTime.SpecifyKind(appointmentEndDateTime, DateTimeKind.Utc),
                    ReminderTime = reminderTime.HasValue ? DateTime.SpecifyKind(reminderTime.Value, DateTimeKind.Utc) : null,
                    Status = AppointmentStatus.Scheduled,
                    MasterProfit = 0,
                    OwnerProfit = 0,
                    DepositPaid = false
                };

                context.Appointments.Add(appointment);
                await context.SaveChangesAsync();

                // Сохраняем дополнительные услуги
                foreach (var addSvc in additionalServices)
                {
                    context.AppointmentServices.Add(new Models.AppointmentService
                    {
                        Id = Guid.NewGuid(),
                        AppointmentId = appointment.Id,
                        ServiceId = addSvc.Id,
                        Price = addSvc.Price,
                        Duration = addSvc.Duration
                    });
                }
                if (additionalServices.Count > 0)
                    await context.SaveChangesAsync();

                await transaction.CommitAsync();

                if (!string.IsNullOrEmpty(master.TelegramId) && !string.IsNullOrEmpty(master.Owner?.BotToken))
                {
                    try
                    {
                        var timeStr = request.AppointmentDate.ToString("HH:mm");
                        var clientName = client.Name ?? "Клиент";
                        var clientPhoneStr = !string.IsNullOrWhiteSpace(client.Phone) ? $" ({client.Phone})" : "";
                        var allServicesStr = service.ServiceName?.Name ?? "услуга";
                        if (additionalServices.Count > 0)
                            allServicesStr += ", " + string.Join(", ", additionalServices.Select(s => s.ServiceName?.Name ?? ""));

                        var message = $"Новая запись!\nК вам записался {clientName}{clientPhoneStr}.\nУслуги: «{allServicesStr}».\nВремя: {timeStr}, длительность {totalDuration} мин.";

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

            var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var mastersList = await context.Masters
                .AsNoTracking()
                .Where(m => m.OwnerId == client.OwnerId && m.IsActive)
                .Select(m => new ClientMasterDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    TelegramId = m.TelegramId,
                    Username = m.TelegramUsername,
                    PhotoUrl = m.PhotoUrl,
                    Description = m.Description,
                    Rating = m.Rating,
                    ReviewsCount = m.Reviews.Count,
                    IsOnVacation = m.Vacations.Any(v => v.StartDate <= todayUtc && v.EndDate >= todayUtc)
                })
                .ToListAsync();

            return Ok(mastersList);
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
        public async Task<IActionResult> GetAvailableTimeSlots([FromQuery] Guid masterId, [FromQuery] Guid serviceId, [FromQuery] DateTime date, [FromQuery] string? additionalServiceIds = null)
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

            var salonTimeZone = GetSalonTimeZone(service.Master.Owner.TimeZone);
            var nowInSalon = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, salonTimeZone);

            // Если дата в прошлом по местному времени заведения — слотов нет
            if (date.Date < nowInSalon.Date)
                return Ok(new List<string>());

            // Проверяем отпуск
            var bookingDateUtc = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            bool onVacation = await context.MasterVacations.AnyAsync(v =>
                v.MasterId == masterId &&
                v.StartDate <= bookingDateUtc &&
                v.EndDate >= bookingDateUtc);
            if (onVacation) return Ok(new List<string>());

            // Суммарная длительность (основная + дополнительные)
            int totalDuration = service.Duration;
            if (!string.IsNullOrWhiteSpace(additionalServiceIds))
            {
                var addIds = additionalServiceIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Guid.TryParse(s.Trim(), out var g) ? (Guid?)g : null)
                    .Where(g => g.HasValue)
                    .Select(g => g!.Value)
                    .Distinct()
                    .ToList();
                foreach (var addId in addIds)
                {
                    if (addId == serviceId) continue;
                    var addSvc = await context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == addId && s.MasterId == masterId && s.IsActive);
                    if (addSvc != null) totalDuration += addSvc.Duration;
                }
            }

            var dayOfWeek = date.DayOfWeek;
            var shifts = await context.Shifts.AsNoTracking()
                .Where(s => s.MasterId == masterId && s.DayOfWeek == dayOfWeek)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            if (shifts.Count == 0) return Ok(new List<string>());

            var startOfDay = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            var appointments = await context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                .Where(a => a.MasterId == masterId
                            && a.Status != AppointmentStatus.Cancelled
                            && a.AppointmentDate < endOfDay
                            && (a.AppointmentEndDate > startOfDay || a.AppointmentDate >= startOfDay))
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var availableSlots = new List<string>();

            foreach (var shift in shifts)
            {
                var currentTime = shift.StartTime;
                int breakMinutes = shift.BreakDurationMinutes;
                int stepMinutes = breakMinutes > 0 
                    ? breakMinutes 
                    : (totalDuration > 0 && totalDuration < 30 ? totalDuration : 30);

                while (currentTime.AddMinutes(totalDuration) <= shift.EndTime)
                {
                    var slotEnd = currentTime.AddMinutes(totalDuration);

                    // Опциональный фиксированный перерыв (если выставлен)
                    if (shift.BreakStartTime.HasValue && shift.BreakEndTime.HasValue)
                    {
                        if (currentTime < shift.BreakEndTime.Value && slotEnd > shift.BreakStartTime.Value)
                        {
                            if (currentTime >= shift.BreakStartTime.Value)
                            {
                                currentTime = shift.BreakEndTime.Value;
                            }
                            else
                            {
                                currentTime = currentTime.AddMinutes(stepMinutes);
                            }
                            continue;
                        }
                    }

                    var slotStartDateTime = startOfDay.Add(currentTime.ToTimeSpan());
                    var slotEndDateTime = startOfDay.Add(slotEnd.ToTimeSpan());
                    var slotEndWithBuffer = slotEndDateTime.AddMinutes(breakMinutes);

                    // Проверка пересечения с существующими записями и перерывами после них
                    bool isOverlap = appointments.Any(a =>
                    {
                        var aStart = a.AppointmentDate;
                        var aEnd = a.AppointmentEndDate > a.AppointmentDate
                            ? a.AppointmentEndDate
                            : a.AppointmentDate.AddMinutes(a.Service?.Duration ?? 60);
                        var aEndWithBuffer = aEnd.AddMinutes(breakMinutes);

                        return slotStartDateTime < aEndWithBuffer && slotEndWithBuffer > aStart;
                    });

                    if (!isOverlap)
                    {
                        // Проверка на прошедшее время в часовом поясе заведения
                        bool isPast = (date.Date == nowInSalon.Date && currentTime <= TimeOnly.FromDateTime(nowInSalon));
                        if (!isPast)
                        {
                            availableSlots.Add(currentTime.ToString("HH:mm"));
                        }
                    }

                    currentTime = currentTime.AddMinutes(stepMinutes);
                }
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
                    MasterTelegramId = a.Master.TelegramId,
                    MasterUsername = a.Master.TelegramUsername,
                    ServiceName = a.Service.ServiceName.Name,
                    AppointmentDate = a.AppointmentDate,
                    AppointmentEndDate = a.AppointmentEndDate,
                    ReminderTime = a.ReminderTime,
                    Status = a.Status,
                    Price = a.Service.Price,
                    HasReview = a.Review != null,
                    PhotoResultUrl = a.PhotoResultUrl
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

            // Валидация рейтинга
            if (review.Rating < 1 || review.Rating > 5)
                return BadRequest(new { message = "Рейтинг должен быть от 1 до 5." });

            if (await context.Reviews.AnyAsync(r => r.AppointmentId == review.AppointmentId))
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
        public string? MasterTelegramId { get; set; }
        public string? MasterUsername { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentEndDate { get; set; }
        public DateTime? ReminderTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public decimal? Price { get; set; }
        public bool HasReview { get; set; }
        public string? PhotoResultUrl { get; set; }
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
