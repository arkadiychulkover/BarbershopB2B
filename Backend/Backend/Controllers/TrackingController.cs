using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class TrackingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly BotService _botService;
        private readonly ILogger<TrackingController> _logger;

        public TrackingController(
            AppDbContext context,
            IConfiguration configuration,
            BotService botService,
            ILogger<TrackingController> logger)
        {
            _context = context;
            _configuration = configuration;
            _botService = botService;
            _logger = logger;
        }

        [HttpPost("landing-visit")]
        public async Task<IActionResult> TrackLandingVisit([FromBody] LandingVisitDto? dto)
        {
            try
            {
                var ip = GetClientIp();
                string userInfo = await ResolveUserInfoAsync(dto?.UserId, dto?.Token);

                var sb = new StringBuilder();
                sb.AppendLine("🌐 Новый визит на SaaS Landing!");
                sb.AppendLine();
                sb.AppendLine($"👤 {userInfo}");
                sb.AppendLine($"🌐 IP: {ip}");
                if (!string.IsNullOrWhiteSpace(dto?.UserAgent))
                {
                    var ua = dto.UserAgent.Length > 150 ? dto.UserAgent.Substring(0, 150) + "..." : dto.UserAgent;
                    sb.AppendLine($"📱 Устройство: {ua}");
                }
                sb.AppendLine($"🕒 Время: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss} UTC");

                await SendTelegramNotificationAsync(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track landing visit");
            }

            return Ok(new { success = true });
        }

        [HttpPost("report-error")]
        public async Task<IActionResult> ReportFrontendError([FromBody] FrontendErrorDto? dto)
        {
            try
            {
                var ip = GetClientIp();
                var sb = new StringBuilder();
                sb.AppendLine($"🚨 Ошибка на фронтенде ({dto?.Source ?? "TMA"})!");
                sb.AppendLine();
                sb.AppendLine($"📍 Запрос: {dto?.Method ?? "GET"} {dto?.Endpoint ?? "/"}");
                if (!string.IsNullOrWhiteSpace(dto?.Status))
                    sb.AppendLine($"📊 Статус: {dto.Status}");
                if (!string.IsNullOrWhiteSpace(dto?.Error))
                    sb.AppendLine($"⚠️ Ошибка: {dto.Error}");
                if (!string.IsNullOrWhiteSpace(dto?.ResponseBody))
                {
                    var resp = dto.ResponseBody.Length > 800 ? dto.ResponseBody.Substring(0, 800) + "..." : dto.ResponseBody;
                    sb.AppendLine($"💬 Ответ сервера: {resp}");
                }
                if (!string.IsNullOrWhiteSpace(dto?.RequestBody))
                {
                    var req = dto.RequestBody.Length > 400 ? dto.RequestBody.Substring(0, 400) + "..." : dto.RequestBody;
                    sb.AppendLine($"📦 Тело запроса: {req}");
                }
                sb.AppendLine($"🌐 IP: {ip}");
                sb.AppendLine($"🕒 Время: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss} UTC");

                await SendTelegramNotificationAsync(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to report frontend error");
            }

            return Ok(new { success = true });
        }

        private string GetClientIp()
        {
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var clientIp = forwardedFor.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(clientIp)) return clientIp;
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        }

        private async Task<string> ResolveUserInfoAsync(string? userId, string? token)
        {
            Guid? targetGuid = null;

            if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var parsedGuid))
            {
                targetGuid = parsedGuid;
            }
            else if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);
                        var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == "sub")?.Value;
                        if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var tokenGuid))
                        {
                            targetGuid = tokenGuid;
                        }
                    }
                }
                catch
                {
                    // Ignore JWT parsing failures
                }
            }

            if (targetGuid.HasValue)
            {
                var owner = await _context.BarbershopOwners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == targetGuid.Value);
                if (owner != null)
                {
                    var shopName = string.IsNullOrWhiteSpace(owner.BarbershopName) ? "" : $" (Барбершоп: {owner.BarbershopName})";
                    var phone = string.IsNullOrWhiteSpace(owner.PhoneNumber) ? "" : $"\n📱 Тел: {owner.PhoneNumber}";
                    return $"Пользователь (Владелец): {owner.OwnerName}{shopName}\n📧 Email: {owner.Email}{phone}\n🔑 ID: {owner.Id}";
                }

                var master = await _context.Masters.AsNoTracking().FirstOrDefaultAsync(m => m.Id == targetGuid.Value);
                if (master != null)
                {
                    var tgUser = string.IsNullOrWhiteSpace(master.TelegramUsername) ? "" : $" (@{master.TelegramUsername})";
                    return $"Пользователь (Мастер): {master.Name}{tgUser}\n🔑 ID: {master.Id}";
                }

                var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == targetGuid.Value);
                if (client != null)
                {
                    var tg = string.IsNullOrWhiteSpace(client.TelegramUsername) ? "" : $" (@{client.TelegramUsername})";
                    var phone = string.IsNullOrWhiteSpace(client.Phone) ? "" : $" Тел: {client.Phone}";
                    return $"Пользователь (Клиент): {client.Name}{tg}{phone}\n🔑 ID: {client.Id}";
                }

                var admin = await _context.SaasAdmins.AsNoTracking().FirstOrDefaultAsync(a => a.Id == targetGuid.Value);
                if (admin != null)
                {
                    return $"Пользователь (Администратор платформы): {admin.Email}\n🔑 ID: {admin.Id}";
                }
            }

            return "👤 Гость (новый посетитель)";
        }

        private async Task SendTelegramNotificationAsync(string message)
        {
            var botToken = _configuration["TelegramBotToken"];
            var adminChatId = _configuration.GetValue<long?>("AdminTelegramChatId") ?? 8558329030;

            if (string.IsNullOrWhiteSpace(botToken)) return;

            if (message.Length > 4000)
            {
                message = message.Substring(0, 4000);
            }

            await _botService.SendMessageAsync(botToken, adminChatId, message);
        }
    }

    public class LandingVisitDto
    {
        public string? UserId { get; set; }
        public string? Token { get; set; }
        public string? UserAgent { get; set; }
    }

    public class FrontendErrorDto
    {
        public string? Endpoint { get; set; }
        public string? Method { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
        public string? ResponseBody { get; set; }
        public string? RequestBody { get; set; }
        public string? Source { get; set; }
    }
}
