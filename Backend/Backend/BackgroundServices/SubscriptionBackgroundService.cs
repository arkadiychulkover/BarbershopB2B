using Backend.Data;
using Backend.Interfaces;
using Backend.Models.Enums;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Backend.BackgroundServices
{
    public class SubscriptionBackgroundService : BackgroundService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly BotService _botService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SubscriptionBackgroundService> _logger;
        private readonly ConcurrentDictionary<Guid, DateTime> _notifiedOwners = new();

        public SubscriptionBackgroundService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            BotService botService,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration,
            ILogger<SubscriptionBackgroundService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _botService = botService;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SubscriptionBackgroundService started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await CheckSubscriptionsAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while checking owner subscriptions.");
                    }

                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task CheckSubscriptionsAsync(CancellationToken stoppingToken)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(stoppingToken);
            var nowUtc = DateTime.UtcNow;

            var activeOwners = await context.BarbershopOwners
                .Where(o => o.Status == OwnerStatus.Active)
                .ToListAsync(stoppingToken);

            int expiredCount = 0;
            int warnedCount = 0;
            var warningThreshold = nowUtc.AddHours(48);

            using var scope = _serviceScopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetService<IEmailService>();

            foreach (var owner in activeOwners)
            {
                if (owner.NextPayment <= nowUtc)
                {
                    owner.Status = OwnerStatus.Frozen;
                    expiredCount++;
                    _notifiedOwners.TryRemove(owner.Id, out _);
                    _logger.LogWarning($"Owner subscription expired: OwnerId={owner.Id}, Name={owner.BarbershopName}, NextPayment={owner.NextPayment:u}. Status changed to Frozen.");
                }
                else if (owner.NextPayment <= warningThreshold)
                {
                    if (!_notifiedOwners.TryGetValue(owner.Id, out var lastNotified) || lastNotified != owner.NextPayment)
                    {
                        _notifiedOwners[owner.Id] = owner.NextPayment;
                        warnedCount++;

                        var timeStr = owner.NextPayment.ToString("dd.MM.yyyy HH:mm");
                        var tgMessage = $"⚠️ Внимание! Срок действия вашей подписки для заведения \"{owner.BarbershopName}\" истекает через 48 часов ({timeStr} UTC).\n\nПожалуйста, продлите подписку в панели управления, чтобы онлайн-запись клиентов и Telegram-бот продолжали функционировать без перебоев.";

                        if (!string.IsNullOrWhiteSpace(owner.TelegramId) && long.TryParse(owner.TelegramId, out long chatId))
                        {
                            var botToken = !string.IsNullOrWhiteSpace(owner.BotToken)
                                ? owner.BotToken
                                : _configuration["TelegramBotToken"];

                            if (!string.IsNullOrWhiteSpace(botToken))
                            {
                                try
                                {
                                    await _botService.SendMessageAsync(botToken, chatId, tgMessage);
                                    _logger.LogInformation("Sent 48h subscription expiration warning to Telegram for owner {OwnerId}", owner.Id);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to send Telegram subscription warning to owner {OwnerId}", owner.Id);
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(owner.Email) && emailService != null)
                        {
                            try
                            {
                                await emailService.SendSubscriptionExpirationWarningAsync(owner.Email, owner.OwnerName, owner.BarbershopName, owner.NextPayment);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send email subscription warning to owner {OwnerId}", owner.Id);
                            }
                        }
                    }
                }
            }

            if (expiredCount > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Subscription check completed. Marked {expiredCount} owner(s) as Frozen.");
            }
            else
            {
                _logger.LogInformation("Subscription check completed. All active subscriptions are valid.");
            }

            if (warnedCount > 0)
            {
                _logger.LogInformation($"Subscription warning sent to {warnedCount} owner(s).");
            }
        }
    }
}
