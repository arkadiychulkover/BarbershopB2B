using Backend.Data;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
                platformWalletAddress = _configuration["Ton:Address"]
            });
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> VerifyPayment([FromBody] string txhHash)
        {
            if (string.IsNullOrWhiteSpace(txhHash))
                return BadRequest("Transaction hash is required.");

            var cleanHash = txhHash.Trim();

            bool isValid = await _ton.CheckTranzaction(cleanHash, _subscriptionAmount);
            if (!isValid)
                return BadRequest("Invalid or expired transaction.");

            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound("User not found.");

            owner.LastPayment = DateTime.UtcNow;
            owner.PayedAt = DateTime.UtcNow;
            owner.NextPayment = DateTime.UtcNow.AddMonths(1);
            owner.Status = OwnerStatus.Active;

            // Persist the transaction record to prevent replay/double-spend attacks
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
            return Ok("Payment verified and subscription activated.");
        }
    }
}
