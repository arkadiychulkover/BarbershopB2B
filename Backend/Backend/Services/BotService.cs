using Telegram.Bot;

namespace Backend.Services
{
    public class BotService
    {
        public async Task SendMessageAsync(string botToken, long chatId, string message)
        {
            var client = new TelegramBotClient(botToken);
            await client.SendMessage(chatId, message);
        }
    }
}
