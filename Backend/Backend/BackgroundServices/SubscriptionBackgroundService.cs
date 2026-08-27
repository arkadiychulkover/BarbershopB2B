using Backend.Data;
using Backend.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.BackgroundServices
{
    public class SubscriptionBackgroundService : BackgroundService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<SubscriptionBackgroundService> _logger;

        public SubscriptionBackgroundService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<SubscriptionBackgroundService> logger)
        {
            _dbContextFactory = dbContextFactory;
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
                // Graceful shutdown
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
            foreach (var owner in activeOwners)
            {
                if (owner.NextPayment <= nowUtc)
                {
                    owner.Status = OwnerStatus.Frozen;
                    expiredCount++;
                    _logger.LogWarning($"Owner subscription expired: OwnerId={owner.Id}, Name={owner.BarbershopName}, NextPayment={owner.NextPayment:u}. Status changed to Frozen.");
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
        }
    }
}
