using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace Backend.Services
{
    public class BotService
    {
        private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;

        public BotService(IDbContextFactory<AppDbContext>? dbContextFactory = null)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task SendMessageAsync(string botToken, long chatId, string message)
        {
            var client = new TelegramBotClient(botToken);
            await client.SendMessage(chatId, message);
        }

        public async Task SendBatchMessagesAsync(
            string botToken,
            Dictionary<long, string> messages,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(botToken) || messages == null || messages.Count == 0)
                return;

            TelegramBotClient client;
            try
            {
                client = new TelegramBotClient(botToken);
            }
            catch
            {
                return;
            }

            HashSet<string> blockedChatIds = new();
            if (_dbContextFactory != null)
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var stringIds = messages.Keys.Select(k => k.ToString()).ToList();
                var blockedClients = await context.Clients
                    .AsNoTracking()
                    .Where(c => stringIds.Contains(c.TelegramId) && c.HasBlocked)
                    .Select(c => c.TelegramId)
                    .ToListAsync(cancellationToken);
                blockedChatIds = new HashSet<string>(blockedClients);
            }

            var validMessages = messages
                .Where(kvp => !blockedChatIds.Contains(kvp.Key.ToString()))
                .ToList();

            using var semaphore = new SemaphoreSlim(20, 20);
            var tasks = validMessages.Select(async kvp =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await SendMessageWithRetryAsync(client, kvp.Key, kvp.Value, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task SendMessageWithRetryAsync(
            ITelegramBotClient client,
            long chatId,
            string message,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await client.SendMessage(chatId, message, cancellationToken: cancellationToken);
                    return;
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 429)
                {
                    var retrySeconds = ex.Parameters?.RetryAfter ?? 1;
                    await Task.Delay(TimeSpan.FromSeconds(retrySeconds), cancellationToken);
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 403)
                {
                    await MarkClientAsBlockedAsync(chatId);
                    return;
                }
                catch
                {
                    return;
                }
            }
        }

        private async Task MarkClientAsBlockedAsync(long chatId)
        {
            if (_dbContextFactory == null) return;
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var tgIdStr = chatId.ToString();
                var client = await context.Clients.FirstOrDefaultAsync(c => c.TelegramId == tgIdStr);
                if (client != null && !client.HasBlocked)
                {
                    client.HasBlocked = true;
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
            }
        }
    }
}
