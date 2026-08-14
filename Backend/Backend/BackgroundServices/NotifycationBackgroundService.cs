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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing reminders.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
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
                .Where(a => a.Status == AppointmentStatus.Scheduled)
                .ToListAsync(stoppingToken);

            foreach (var appointment in appointmentsToProcess)
            {
                var owner = appointment.Client?.Owner;
                if (owner == null || !owner.HasActiveSubscription() || string.IsNullOrEmpty(owner.BotToken)) continue;

                var ownerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(owner.TimeZone);
                var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ownerTimeZone);
                var nowLocalUtcKind = DateTime.SpecifyKind(nowLocal, DateTimeKind.Utc);
                
                var targetTime = appointment.AppointmentDate.Subtract(TimeSpan.FromHours(owner.ReminderHoursBefore));

                if (!appointment.ReminderSent && nowLocalUtcKind >= targetTime && nowLocalUtcKind < appointment.AppointmentDate)
                {
                    if (!string.IsNullOrEmpty(appointment.Client.TelegramId))
                    {
                        try
                        {
                            var timeStr = appointment.AppointmentDate.ToString("HH:mm");

                            var message = $"🔔 Напоминание: у вас запланирована запись к мастеру {appointment.Master.Name} на сегодня в {timeStr}. Ждем вас!";
                            
                            if(long.TryParse(appointment.Client.TelegramId, out long chatId))
                            {
                                await _botService.SendMessageAsync(owner.BotToken, chatId, message);
                                appointment.ReminderSent = true;
                                _logger.LogInformation($"Sent reminder for appointment {appointment.Id} to client {appointment.ClientId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send reminder for appointment {appointment.Id}");
                        }
                    }
                    else
                    {
                        appointment.ReminderSent = true;
                    }
                }
                else if (nowLocalUtcKind >= appointment.AppointmentEndDate)
                {
                    appointment.Status = AppointmentStatus.Completed;
                    
                    if (!string.IsNullOrEmpty(appointment.Client.TelegramId) && long.TryParse(appointment.Client.TelegramId, out long chatId))
                    {
                        try
                        {
                            var reviewMessage = $"Спасибо за визит к мастеру {appointment.Master.Name}! Пожалуйста, оцените работу мастера в приложении.";
                            await _botService.SendMessageAsync(owner.BotToken, chatId, reviewMessage);
                            _logger.LogInformation($"Sent review request for appointment {appointment.Id} to client {appointment.ClientId}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send review request for appointment {appointment.Id}");
                        }
                    }
                }
            }

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
