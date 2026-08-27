using Backend.Data;
using Backend.Models.Enums;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.BackgroundServices
{
    public class NotifycationBackgroundService : BackgroundService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly BotService _botService;
        private readonly ILogger<NotifycationBackgroundService> _logger;

        public NotifycationBackgroundService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            BotService botService,
            ILogger<NotifycationBackgroundService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _botService = botService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationBackgroundService starting.");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessRemindersAsync(stoppingToken);
                        await ProcessWinBackAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred processing notifications.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
        }

        private async Task ProcessRemindersAsync(CancellationToken stoppingToken)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(stoppingToken);
            
            var nowUtc = DateTime.UtcNow;

            var appointmentsToProcess = await context.Appointments
                .Include(a => a.Client)
                    .ThenInclude(c => c.Owner)
                .Include(a => a.Master)
                .Include(a => a.Service)
                    .ThenInclude(s => s.ServiceName)
                .Where(a => a.Status == AppointmentStatus.Scheduled && a.AppointmentDate > nowUtc)
                .ToListAsync(stoppingToken);

            foreach (var appointment in appointmentsToProcess)
            {
                var owner = appointment.Client?.Owner;
                if (owner == null || !owner.HasActiveSubscription() || string.IsNullOrEmpty(owner.BotToken)) continue;

                var ownerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(owner.TimeZone);
                var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ownerTimeZone);
                var nowLocalUtcKind = DateTime.SpecifyKind(nowLocal, DateTimeKind.Utc);
                
                var targetTime = appointment.ReminderTime ?? appointment.AppointmentDate.Subtract(TimeSpan.FromHours(owner.ReminderHoursBefore));

                // Обычное напоминание о записи
                if (!appointment.ReminderSent && nowLocalUtcKind >= targetTime && nowLocalUtcKind < appointment.AppointmentDate)
                {
                    if (!string.IsNullOrEmpty(appointment.Client.TelegramId))
                    {
                        try
                        {
                            var timeStr = appointment.AppointmentDate.ToString("HH:mm");
                            var message = $"🔔 Напоминание: у вас запланирована запись к мастеру {appointment.Master.Name} на сегодня в {timeStr}. Ждем вас!";
                            
                            if (long.TryParse(appointment.Client.TelegramId, out long chatId))
                            {
                                await _botService.SendMessageAsync(owner.BotToken, chatId, message);
                                appointment.ReminderSent = true;
                                _logger.LogInformation("Sent reminder for appointment {AppointmentId}", appointment.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send reminder for appointment {AppointmentId}", appointment.Id);
                        }
                    }
                    else
                    {
                        appointment.ReminderSent = true;
                    }
                }
            }

            // Автозавершение и отправка запроса на отзыв
            var completedAppointments = await context.Appointments
                .Include(a => a.Client).ThenInclude(c => c.Owner)
                .Include(a => a.Master)
                .Where(a => a.Status == AppointmentStatus.Scheduled && a.AppointmentEndDate <= nowUtc)
                .ToListAsync(stoppingToken);

            foreach (var appointment in completedAppointments)
            {
                appointment.Status = AppointmentStatus.Completed;

                var owner = appointment.Client?.Owner;
                if (owner == null || !owner.HasActiveSubscription() || string.IsNullOrEmpty(owner.BotToken)) continue;

                if (!string.IsNullOrEmpty(appointment.Client.TelegramId) && long.TryParse(appointment.Client.TelegramId, out long chatId))
                {
                    try
                    {
                        var reviewMessage = $"Спасибо за визит к мастеру {appointment.Master.Name}! Пожалуйста, оцените работу мастера в приложении.";
                        await _botService.SendMessageAsync(owner.BotToken, chatId, reviewMessage);
                        _logger.LogInformation("Sent review request for appointment {AppointmentId}", appointment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send review request for appointment {AppointmentId}", appointment.Id);
                    }
                }
            }

            if (context.ChangeTracker.HasChanges())
                await context.SaveChangesAsync(stoppingToken);
        }

        /// <summary>Win-back: отправляем напоминание клиентам, которые давно не стриглись.</summary>
        private async Task ProcessWinBackAsync(CancellationToken stoppingToken)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(stoppingToken);

            var owners = await context.BarbershopOwners
                .AsNoTracking()
                .Where(o => o.WinBackDays > 0 && o.Status == Models.Enums.OwnerStatus.Active && !o.IsBlocked)
                .ToListAsync(stoppingToken);

            foreach (var owner in owners)
            {
                if (string.IsNullOrEmpty(owner.BotToken)) continue;

                var cutoff = DateTime.UtcNow.AddDays(-owner.WinBackDays);

                // Клиенты этого заведения, чья последняя завершённая запись старше cutoff и которым ещё не отправляли win-back в этом периоде
                var sleepingClients = await context.Clients
                    .Where(c => c.OwnerId == owner.Id
                        && !string.IsNullOrEmpty(c.TelegramId)
                        && !c.IsBlacklisted
                        && (c.LastWinBackSentAt == null || c.LastWinBackSentAt < cutoff)
                        && c.Appointments.Any(a => a.Status == AppointmentStatus.Completed)
                        && c.Appointments
                            .Where(a => a.Status == AppointmentStatus.Completed)
                            .Max(a => a.AppointmentDate) < cutoff)
                    .ToListAsync(stoppingToken);

                foreach (var client in sleepingClients)
                {
                    if (!long.TryParse(client.TelegramId, out long chatId)) continue;
                    try
                    {
                        var daysText = owner.WinBackDays switch
                        {
                            30 => "месяц",
                            60 => "два месяца",
                            90 => "три месяца",
                            _ => $"{owner.WinBackDays} дн."
                        };
                        var msg = $"👋 Здравствуйте, {client.Name}!\nВы уже {daysText} не были у нас в «{owner.BarbershopName}». Не хотели бы записаться на стрижку? 😊";
                        await _botService.SendMessageAsync(owner.BotToken, chatId, msg);
                        client.LastWinBackSentAt = DateTime.UtcNow;
                        _logger.LogInformation("Sent win-back to client {TelegramId} for owner {OwnerId}", client.TelegramId, owner.Id);
                    }
                    catch { }
                }

                if (context.ChangeTracker.HasChanges())
                    await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
