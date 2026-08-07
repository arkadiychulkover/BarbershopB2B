using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
            return Ok(new { 
                price = _subscriptionAmount,
                platformWalletAddress = _configuration["Ton:Address"]
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> VerifyPayment([FromBody] string txhHash)
        {
            if (string.IsNullOrWhiteSpace(txhHash))
            {
                return BadRequest("Transaction hash is required.");
            }
            bool isValid = await _ton.CheckTranzaction(txhHash, _subscriptionAmount);
            if (!isValid)
            {
                return BadRequest("Invalid transaction.");
            }
            var userId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(userId));
            if (owner == null)
            {
                return NotFound("User not found.");
            }

            owner.LastPayment = DateTime.UtcNow;
            owner.PayedAt = DateTime.UtcNow;
            owner.NextPayment = DateTime.UtcNow.AddMonths(1);
            owner.Status = Backend.Models.Enums.OwnerStatus.Active;

            await _context.SaveChangesAsync();
            return Ok("Payment verified and subscription activated.");
        }
    }
}
