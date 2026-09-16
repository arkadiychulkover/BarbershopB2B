using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("StrictAuthPolicy")]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly decimal _subscriptionAmount;
        private readonly AppDbContext _context;
        private readonly TonService _ton;

        public PaymentController(AppDbContext context, TonService ton, IConfiguration configuration)
        {
            _context = context;
            _ton = ton;
            _configuration = configuration;
            _subscriptionAmount = Convert.ToDecimal(_configuration["SubscriptionAmount"], System.Globalization.CultureInfo.InvariantCulture);
        }

        [HttpGet("price")]
        [AllowAnonymous]
        public IActionResult GetSubscriptionPrice()
        {
            return Ok(new
            {
                price = _subscriptionAmount,
                amount = _subscriptionAmount,
                platformWalletAddress = _configuration["Ton:Address"]
            });
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> VerifyPayment([FromBody] object payload)
        {
            return await ProcessSubscriptionPayment(payload);
        }

        [HttpPost("renew")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> RenewSubscription([FromBody] object payload)
        {
            return await ProcessSubscriptionPayment(payload);
        }

        [HttpPost("redeem-key")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> RedeemPromoKey([FromBody] RedeemPromoKeyRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Key))
                return BadRequest("Ключ обязателен для ввода.");

            var rawKey = request.Key.Trim();

            var activeKeys = await _context.PromoKeys
                .Where(p => p.Status == PromoKeyStatus.Created)
                .ToListAsync();

            var matchedKey = activeKeys.FirstOrDefault(p => PromoKeyService.VerifyKey(rawKey, p.Salt, p.KeyHash));
            if (matchedKey == null)
                return BadRequest("Недействительный или уже использованный ключ.");

            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound("User not found.");

            var baseDate = (owner.NextPayment > DateTime.UtcNow)
                ? owner.NextPayment
                : DateTime.UtcNow;

            owner.LastPayment = DateTime.UtcNow;
            owner.PayedAt = DateTime.UtcNow;
            owner.NextPayment = baseDate.AddDays(matchedKey.Days);
            owner.Status = OwnerStatus.Active;
            owner.IsBlocked = false;

            matchedKey.Status = PromoKeyStatus.Used;
            matchedKey.UsedAt = DateTime.UtcNow;
            matchedKey.UsedByOwnerId = owner.Id;

            var transaction = new Tranzaction
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                TxhHash = $"PROMO-{matchedKey.Id.ToString("N")[..8].ToUpperInvariant()}",
                Amount = 0,
                Time = DateTime.UtcNow
            };
            _context.Tranxactions.Add(transaction);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Подписка успешно продлена на {matchedKey.Days} дн.",
                days = matchedKey.Days,
                nextPayment = owner.NextPayment,
                lastPayment = owner.LastPayment,
                status = owner.Status.ToString(),
                isSubscribed = true
            });
        }

        private async Task<IActionResult> ProcessSubscriptionPayment(object payload)
        {
            string txHash = null;

            if (payload is System.Text.Json.JsonElement element)
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    txHash = element.GetString();
                }
                else if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (element.TryGetProperty("txHash", out var prop) ||
                        element.TryGetProperty("txhHash", out prop) ||
                        element.TryGetProperty("hash", out prop))
                    {
                        txHash = prop.GetString();
                    }
                }
            }
            else if (payload is string str)
            {
                txHash = str;
            }

            if (string.IsNullOrWhiteSpace(txHash))
                return BadRequest("Transaction hash is required.");

            var cleanHash = txHash.Trim();

            bool isValid = await _ton.CheckTranzaction(cleanHash, _subscriptionAmount);
            if (!isValid)
                return BadRequest("Invalid or expired transaction.");

            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound("User not found.");

            var baseDate = (owner.NextPayment > DateTime.UtcNow)
                ? owner.NextPayment
                : DateTime.UtcNow;

            owner.LastPayment = DateTime.UtcNow;
            owner.PayedAt = DateTime.UtcNow;
            owner.NextPayment = baseDate.AddMonths(1);
            owner.Status = OwnerStatus.Active;

            var transaction = new Tranzaction
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                TxhHash = cleanHash,
                Amount = _subscriptionAmount,
                Time = DateTime.UtcNow
            };
            _context.Tranxactions.Add(transaction);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Подписка успешно продлена на 1 месяц.",
                nextPayment = owner.NextPayment,
                lastPayment = owner.LastPayment,
                status = owner.Status.ToString(),
                isSubscribed = true
            });
        }
    }
}
