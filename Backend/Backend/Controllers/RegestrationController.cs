using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Interfaces;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("StrictAuthPolicy")]
    public class RegestrationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public RegestrationController(AppDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            bool emailExists = await _context.BarbershopOwners.AnyAsync(o => o.Email.ToLower() == cleanEmail);
            if (emailExists)
            {
                return BadRequest(new { message = "Пользователь с таким email уже зарегистрирован." });
            }

            string? cleanedPhone = null;
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                cleanedPhone = Regex.Replace(request.PhoneNumber.Trim(), @"[\s\-\(\)]", "");
                var phoneRegex = new Regex(@"^\+[0-9]{1,3}[0-9]{9}$");
                if (!phoneRegex.IsMatch(cleanedPhone))
                {
                    return BadRequest(new { message = "Некорректный номер телефона. Номер должен начинаться с \"+\", содержать код страны (1-3 цифры) и 9 цифр номера (например, +380991234567 или +79991234567)." });
                }
            }

            string passwordHash = PasswordSecurity.HashPassword(request.Password);

            _context.BarbershopOwners.Add(new BarbershopOwner
            {
                OwnerName = request.OwnerName?.Trim(),
                PhoneNumber = cleanedPhone ?? "",
                Email = cleanEmail,
                TelegramId = request.TelegramId?.Trim(),
                BarbershopName = request.BarbershopName?.Trim(),
                BarbershopAddress = request.BarbershopAddress?.Trim(),
                BarbershopDescription = request.BarbershopDescription?.Trim(),
                BotToken = request.BotToken?.Trim(),
                BotUsername = request.BotUsername?.Trim(),
                TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Europe/Kyiv" : request.TimeZone.Trim(),
                PasswordHash = passwordHash,
                WalletAddress = ""
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Registration successful" });
        }

        [HttpPost("change-password")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var ownerId = User.GetUserId();
            if (ownerId == Guid.Empty)
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound();

            if (!PasswordSecurity.VerifyPassword(request.CurrentPassword, owner.PasswordHash))
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            owner.PasswordHash = PasswordSecurity.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Укажите адрес электронной почты." });
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();

            // Find either an Owner or an Admin
            var owner = await _context.BarbershopOwners.FirstOrDefaultAsync(o => o.Email.ToLower() == cleanEmail);
            var admin = owner == null ? await _context.SaasAdmins.FirstOrDefaultAsync(a => a.Email.ToLower() == cleanEmail) : null;

            if (owner == null && admin == null)
            {
                return Ok(new { message = "Если аккаунт с указанным email существует, ссылка для сброса пароля отправлена в Telegram." });
            }

            // Generate cryptographically secure token
            string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            DateTime tokenExpiry = DateTime.UtcNow.AddHours(2);

            string recipientName;
            string? telegramId = null;

            if (owner != null)
            {
                owner.PasswordResetToken = token;
                owner.PasswordResetTokenExpires = tokenExpiry;
                recipientName = owner.OwnerName ?? "Владелец заведения";
                telegramId = owner.TelegramId;
            }
            else
            {
                admin!.PasswordResetToken = token;
                admin.PasswordResetTokenExpires = tokenExpiry;
                recipientName = "Администратор";
            }

            // Telegram is required for password reset
            if (string.IsNullOrWhiteSpace(telegramId) || !long.TryParse(telegramId, out long chatId))
            {
                return BadRequest(new { message = "К этому аккаунту не привязан Telegram. Обратитесь к администратору для сброса пароля." });
            }

            await _context.SaveChangesAsync();

            // Determine frontend base URL
            string frontendBase = _configuration["FrontendUrl"] ?? "http://localhost:5173";
            if (Request.Headers.TryGetValue("Origin", out var origin) && !string.IsNullOrWhiteSpace(origin))
            {
                frontendBase = origin.ToString().TrimEnd('/');
            }
            else if (Request.Headers.TryGetValue("Referer", out var referer) && !string.IsNullOrWhiteSpace(referer))
            {
                try
                {
                    var uri = new Uri(referer.ToString());
                    frontendBase = $"{uri.Scheme}://{uri.Authority}";
                }
                catch { }
            }

            string resetLink = $"{frontendBase}/#/reset-password?token={Uri.EscapeDataString(token)}";

            try
            {
                string platformBotToken = _configuration["TelegramBotToken"] ?? "";
                var botService = HttpContext.RequestServices.GetRequiredService<BotService>();

                string tgMessage = $"🔐 *Восстановление пароля*\n\n" +
                    $"Здравствуйте, {recipientName}!\n\n" +
                    $"Вы запросили сброс пароля для аккаунта на платформе BarbershopB2B\\.\n\n" +
                    $"👉 [Нажмите здесь, чтобы сбросить пароль]({resetLink})\n\n" +
                    $"⏱ Ссылка действительна 2 часа\\.\n\n" +
                    $"Если вы не запрашивали сброс — проигнорируйте это сообщение\\.";

                await botService.SendMessageAsync(platformBotToken, chatId, tgMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Не удалось отправить сообщение в Telegram: " + ex.Message });
            }

            return Ok(new { message = "Ссылка для сброса пароля отправлена вам в Telegram." });
        }

        [HttpGet("verify-reset-token")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyResetToken([FromQuery] string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { valid = false, message = "Токен восстановления не указан." });
            }

            var cleanToken = token.Trim();
            var owner = await _context.BarbershopOwners.FirstOrDefaultAsync(o => o.PasswordResetToken == cleanToken && o.PasswordResetTokenExpires > DateTime.UtcNow);
            if (owner != null)
            {
                return Ok(new { valid = true, email = MaskEmail(owner.Email) });
            }

            var admin = await _context.SaasAdmins.FirstOrDefaultAsync(a => a.PasswordResetToken == cleanToken && a.PasswordResetTokenExpires > DateTime.UtcNow);
            if (admin != null)
            {
                return Ok(new { valid = true, email = MaskEmail(admin.Email) });
            }

            return BadRequest(new { valid = false, message = "Ссылка для смены пароля недействительна, устарела или уже была использована." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { message = "Токен восстановления не указан." });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Пароль должен содержать минимум 6 символов." });
            }

            var cleanToken = request.Token.Trim();
            var owner = await _context.BarbershopOwners.FirstOrDefaultAsync(o => o.PasswordResetToken == cleanToken && o.PasswordResetTokenExpires > DateTime.UtcNow);
            if (owner != null)
            {
                owner.PasswordHash = PasswordSecurity.HashPassword(request.NewPassword);
                owner.PasswordResetToken = null;
                owner.PasswordResetTokenExpires = null;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Пароль успешно изменен. Теперь вы можете войти в систему с новым паролем." });
            }

            var admin = await _context.SaasAdmins.FirstOrDefaultAsync(a => a.PasswordResetToken == cleanToken && a.PasswordResetTokenExpires > DateTime.UtcNow);
            if (admin != null)
            {
                var (salt, hash) = PasswordSecurity.CreateHashAndSalt(request.NewPassword);
                admin.PasswordSalt = salt;
                admin.PasswordHash = hash;
                admin.PasswordResetToken = null;
                admin.PasswordResetTokenExpires = null;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Пароль успешно изменен. Теперь вы можете войти в систему с новым паролем." });
            }

            return BadRequest(new { message = "Токен недействителен, устарел или уже был использован." });
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return email;
            var parts = email.Split('@');
            var name = parts[0];
            var domain = parts[1];
            if (name.Length <= 2) return $"{name[0]}*@{domain}";
            return $"{name[0]}***{name[^1]}@{domain}";
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("Secret");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var admin = await _context.SaasAdmins.FirstOrDefaultAsync(a => a.Email.ToLower() == cleanEmail);
            if (admin != null)
            {
                bool isValidAdmin = PasswordSecurity.VerifyPasswordWithSalt(request.Password, admin.PasswordSalt, admin.PasswordHash)
                                    || PasswordSecurity.VerifyPassword(request.Password, admin.PasswordHash);

                if (isValidAdmin)
                {
                    var adminClaims = new[]
                    {
                        new Claim("UserId", admin.Id.ToString()),
                        new Claim(ClaimTypes.Role, "Admin"),
                        new Claim(JwtRegisteredClaimNames.Sub, admin.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };

                    var adminToken = new JwtSecurityToken(
                        issuer: jwtSettings.GetValue<string>("Issuer"),
                        audience: jwtSettings.GetValue<string>("Audience"),
                        claims: adminClaims,
                        expires: DateTime.UtcNow.AddDays(7),
                        signingCredentials: creds
                    );

                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(adminToken),
                        expiration = adminToken.ValidTo,
                        role = "Admin"
                    });
                }
            }

            var owner = await _context.BarbershopOwners.FirstOrDefaultAsync(o => o.Email.ToLower() == cleanEmail);
            if (owner == null) return Unauthorized(new { message = "Invalid email or password" });

            if (!PasswordSecurity.VerifyPassword(request.Password, owner.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password" });

            if (PasswordSecurity.NeedsUpgrade(owner.PasswordHash))
            {
                owner.PasswordHash = PasswordSecurity.HashPassword(request.Password);
                await _context.SaveChangesAsync();
            }

            var claims = new[]
            {
                new Claim("UserId", owner.Id.ToString()),
                new Claim(ClaimTypes.Role, "Owner"),
                new Claim(JwtRegisteredClaimNames.Sub, owner.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings.GetValue<string>("Issuer"),
                audience: jwtSettings.GetValue<string>("Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                role = "Owner"
            });
        }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; }
        [Required]
        public string NewPassword { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; }
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
}
