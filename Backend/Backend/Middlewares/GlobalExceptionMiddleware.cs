using Backend.Services;
using System.Text.Json;

namespace Backend.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly BotService _botService;
        private const long AdminTelegramChatId = 8558329030;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IConfiguration configuration,
            BotService botService)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _botService = botService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
                await NotifyAdminAsync(context, ex);
                await HandleExceptionResponseAsync(context, ex);
            }
        }

        private async Task NotifyAdminAsync(HttpContext context, Exception ex)
        {
            try
            {
                var botToken = _configuration["TelegramBotToken"];
                if (string.IsNullOrWhiteSpace(botToken)) return;

                var firstStackLine = ex.StackTrace?
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()?
                    .Trim() ?? "No stack trace";

                var message = $"🚨 Ошибка на сервере!\n\n" +
                              $"📍 Запрос: {context.Request.Method} {context.Request.Path}\n" +
                              $"⚠️ Ошибка: {ex.GetType().Name}\n" +
                              $"💬 Сообщение: {ex.Message}\n" +
                              $"🕒 Время: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss} UTC\n\n" +
                              $"Стек: {firstStackLine}";

                if (message.Length > 4000)
                {
                    message = message.Substring(0, 4000);
                }

                await _botService.SendMessageAsync(botToken, AdminTelegramChatId, message);
            }
            catch (Exception notifyEx)
            {
                _logger.LogError(notifyEx, "Failed to send error notification to Telegram admin.");
            }
        }

        private static async Task HandleExceptionResponseAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = new
            {
                error = "Произошла внутренняя ошибка сервера. Администратор уже уведомлен.",
                details = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
