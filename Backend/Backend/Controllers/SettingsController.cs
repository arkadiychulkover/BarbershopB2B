using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Owner")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound("Owner not found.");

            if (owner.Status == OwnerStatus.Active && owner.NextPayment <= DateTime.UtcNow)
            {
                owner.Status = OwnerStatus.Frozen;
                await _context.SaveChangesAsync();
            }

            var response = new SettingsResponse
            {
                Id = owner.Id,
                Email = owner.Email,
                BarbershopName = owner.BarbershopName,
                BarbershopAddress = owner.BarbershopAddress,
                BarbershopDescription = owner.BarbershopDescription,
                OwnerName = owner.OwnerName,
                TimeZone = owner.TimeZone,
                PhoneNumber = owner.PhoneNumber,
                TelegramId = owner.TelegramId,
                BotToken = owner.BotToken,
                BotUsername = owner.BotUsername,
                ReminderHoursBefore = owner.ReminderHoursBefore,
                WinBackDays = owner.WinBackDays,
                MasterFee = owner.MasterFee,
                WalletAddress = owner.WalletAddress,
                IsSubscribed = owner.HasActiveSubscription(),
                Status = owner.Status.ToString(),
                NextPayment = owner.NextPayment,
                LastPayment = owner.LastPayment
            };

            return Ok(response);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound("Owner not found.");

            if (!owner.HasActiveSubscription())
            {
                if (!string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    owner.WalletAddress = request.WalletAddress.Trim();
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Settings updated successfully." });
                }

                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Изменение настроек заблокировано." });
            }

            if (!string.IsNullOrWhiteSpace(request.BarbershopName))
                owner.BarbershopName = request.BarbershopName.Trim();

            if (request.BarbershopAddress != null)
                owner.BarbershopAddress = request.BarbershopAddress.Trim();

            if (request.BarbershopDescription != null)
                owner.BarbershopDescription = request.BarbershopDescription.Trim();

            if (!string.IsNullOrWhiteSpace(request.OwnerName))
                owner.OwnerName = request.OwnerName.Trim();

            if (!string.IsNullOrWhiteSpace(request.TimeZone))
                owner.TimeZone = request.TimeZone.Trim();

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var cleanedPhone = Regex.Replace(request.PhoneNumber.Trim(), @"[\s\-\(\)]", "");
                var phoneRegex = new Regex(@"^\+[0-9]{1,3}[0-9]{9}$");
                if (!phoneRegex.IsMatch(cleanedPhone))
                {
                    return BadRequest(new { message = "Некорректный номер телефона. Номер должен начинаться с \"+\", содержать код страны (1-3 цифры) и 9 цифр номера (например, +380991234567 или +79991234567)." });
                }
                owner.PhoneNumber = cleanedPhone;
            }
            else if (request.PhoneNumber != null)
            {
                owner.PhoneNumber = "";
            }

            if (request.TelegramId != null)
                owner.TelegramId = request.TelegramId.Trim();

            if (request.BotToken != null)
                owner.BotToken = request.BotToken.Trim();

            if (request.BotUsername != null)
                owner.BotUsername = request.BotUsername.Trim().TrimStart('@');

            if (request.ReminderHoursBefore > 0)
                owner.ReminderHoursBefore = request.ReminderHoursBefore;

            if (request.WinBackDays >= 0)
                owner.WinBackDays = request.WinBackDays;

            if (request.MasterFee >= 0)
                owner.MasterFee = request.MasterFee;

            if (request.WalletAddress != null)
                owner.WalletAddress = request.WalletAddress.Trim();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Settings updated successfully." });
        }

        [HttpPut("wallet")]
        public async Task<IActionResult> UpdateWalletAddress([FromBody] UpdateWalletRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound("Owner not found.");

            if (string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                return BadRequest(new { message = "Адрес кошелька не может быть пустым." });
            }

            owner.WalletAddress = request.WalletAddress.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Кошелек успешно сохранен.", walletAddress = owner.WalletAddress });
        }
    }
}
