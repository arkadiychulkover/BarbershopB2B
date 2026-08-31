using Backend.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    /// <summary>
    /// Sends transactional emails via Brevo (ex-Sendinblue) HTTP API.
    /// Works on Railway and other platforms that block SMTP ports (25/465/587).
    /// Uses HTTPS (port 443) which is never blocked.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string BrevoApiUrl = "https://api.brevo.com/v3/smtp/email";

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private async Task SendViaBrevoAsync(string toEmail, string toName, string subject, string htmlContent)
        {
            string apiKey = _configuration["BrevoApiKey"] ?? "";
            string senderEmail = _configuration.GetSection("SmtpSettings").GetValue<string>("SenderEmail") ?? "arkadijculkov2@gmail.com";
            string senderName = _configuration.GetSection("SmtpSettings").GetValue<string>("SenderName") ?? "BarbershopB2B Platform";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("BrevoApiKey не настроен в appsettings.json. Получите API-ключ на https://app.brevo.com");
            }

            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                htmlContent
            };

            string json = JsonSerializer.Serialize(payload);

            using var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, BrevoApiUrl);
            request.Headers.Add("api-key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Brevo API error {StatusCode}: {Body}", response.StatusCode, responseBody);
                throw new Exception($"Brevo API ошибка ({response.StatusCode}): {responseBody}");
            }

            _logger.LogInformation("Email sent via Brevo to {Email}, response: {Response}", toEmail, responseBody);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetLink)
        {
            string safeName = string.IsNullOrWhiteSpace(recipientName) ? "Пользователь" : recipientName.Trim();

            string htmlBody = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Восстановление пароля</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #0f172a; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #f1f5f9;"">
    <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #0f172a; padding: 40px 15px;"">
        <tr>
            <td align=""center"">
                <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 580px; background-color: #1e293b; border-radius: 16px; border: 1px solid rgba(255,255,255,0.1); overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.5);"">
                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""padding: 35px 30px 25px; background: linear-gradient(135deg, rgba(223,158,142,0.15), rgba(179,183,219,0.1)); border-bottom: 1px solid rgba(255,255,255,0.08);"">
                            <div style=""display: inline-block; padding: 8px 16px; background: rgba(223,158,142,0.15); border: 1px solid rgba(223,158,142,0.3); border-radius: 30px; margin-bottom: 14px;"">
                                <span style=""font-size: 13px; font-weight: 700; color: #df9e8e; letter-spacing: 0.05em; text-transform: uppercase;"">Безопасность аккаунта</span>
                            </div>
                            <h1 style=""margin: 0; font-size: 24px; font-weight: 800; color: #ffffff; letter-spacing: -0.02em;"">Восстановление пароля</h1>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style=""padding: 35px 35px 25px;"">
                            <p style=""margin: 0 0 16px; font-size: 16px; line-height: 1.6; color: #e2e8f0;"">
                                Здравствуйте, <strong style=""color: #df9e8e;"">{WebUtility.HtmlEncode(safeName)}</strong>!
                            </p>
                            <p style=""margin: 0 0 24px; font-size: 15px; line-height: 1.6; color: #94a3b8;"">
                                Мы получили запрос на сброс пароля для вашей учетной записи на платформе. Если вы делали этот запрос, нажмите кнопку ниже, чтобы установить новый пароль:
                            </p>
                            
                            <!-- CTA Button -->
                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{resetLink}"" target=""_blank"" style=""display: inline-block; padding: 15px 36px; background: linear-gradient(135deg, #df9e8e, #c88777); color: #121826; text-decoration: none; font-size: 15px; font-weight: 700; border-radius: 12px; box-shadow: 0 6px 20px rgba(223,158,142,0.35); text-align: center;"">
                                            Сбросить пароль
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <div style=""background: rgba(15,23,42,0.6); border-radius: 10px; border: 1px solid rgba(255,255,255,0.06); padding: 18px; margin-top: 25px;"">
                                <p style=""margin: 0 0 10px; font-size: 13px; font-weight: 600; color: #cbd5e1;"">
                                    Не работает кнопка? Скопируйте и откройте прямую ссылку:
                                </p>
                                <p style=""margin: 0; font-size: 12px; line-height: 1.5; color: #b3b7db; word-break: break-all;"">
                                    <a href=""{resetLink}"" target=""_blank"" style=""color: #b3b7db; text-decoration: underline;"">{resetLink}</a>
                                </p>
                            </div>

                            <div style=""margin-top: 25px; padding-top: 20px; border-top: 1px solid rgba(255,255,255,0.08);"">
                                <p style=""margin: 0 0 8px; font-size: 13px; color: #fca5a5; font-weight: 600;"">
                                    ⏱ Ссылка действительна в течение 2 часов.
                                </p>
                                <p style=""margin: 0; font-size: 13px; color: #64748b; line-height: 1.5;"">
                                    Если вы не отправляли запрос на сброс пароля, просто проигнорируйте это письмо. Ваш пароль останется в безопасности.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding: 20px 30px; background-color: #0f172a; border-top: 1px solid rgba(255,255,255,0.06);"">
                            <p style=""margin: 0; font-size: 12px; color: #64748b;"">
                                &copy; {DateTime.UtcNow.Year} BarbershopB2B Platform. Все права защищены.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            try
            {
                await SendViaBrevoAsync(toEmail, safeName, "Восстановление пароля на платформе", htmlBody);
                _logger.LogInformation("Password reset email successfully sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                throw new Exception($"Не удалось отправить email: {ex.Message}", ex);
            }
        }

        public async Task SendSubscriptionExpirationWarningAsync(string toEmail, string ownerName, string barbershopName, DateTime nextPayment)
        {
            string safeName = string.IsNullOrWhiteSpace(ownerName) ? "Владелец" : ownerName.Trim();
            string safeShop = string.IsNullOrWhiteSpace(barbershopName) ? "вашего заведения" : $"заведения \"{barbershopName.Trim()}\"";
            string dateStr = nextPayment.ToString("dd.MM.yyyy HH:mm");

            string htmlBody = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <title>Истечение срока подписки</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #0f172a; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #f1f5f9;"">
    <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #0f172a; padding: 40px 15px;"">
        <tr>
            <td align=""center"">
                <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 580px; background-color: #1e293b; border-radius: 16px; border: 1px solid rgba(255,255,255,0.1); overflow: hidden;"">
                    <tr>
                        <td align=""center"" style=""padding: 30px; background: rgba(223,158,142,0.15);"">
                            <h1 style=""margin: 0; font-size: 22px; color: #ffffff;"">Истечение срока подписки</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 30px;"">
                            <p style=""font-size: 16px; color: #e2e8f0;"">Здравствуйте, <strong>{WebUtility.HtmlEncode(safeName)}</strong>!</p>
                            <p style=""font-size: 15px; color: #94a3b8; line-height: 1.6;"">
                                Напоминаем, что срок действия подписки для {WebUtility.HtmlEncode(safeShop)} истекает через 48 часов: <strong>{dateStr} (UTC)</strong>.
                            </p>
                            <p style=""font-size: 15px; color: #94a3b8; line-height: 1.6;"">
                                Чтобы онлайн-запись клиентов и Telegram-бот продолжали функционировать без перебоев, пожалуйста, продлите подписку в панели управления.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            try
            {
                await SendViaBrevoAsync(toEmail, safeName, "Напоминание: подписка BarbershopB2B истекает через 48 часов", htmlBody);
                _logger.LogInformation("Subscription warning email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send subscription warning email to {Email}", toEmail);
            }
        }
    }
}
