using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

            var response = new SettingsResponse
            {
                BarbershopName = owner.BarbershopName,
                BarbershopAddress = owner.BarbershopAddress,
                BarbershopDescription = owner.BarbershopDescription,
                OwnerName = owner.OwnerName,
                TimeZone = owner.TimeZone,
                LogoUrl = owner.LogoUrl,
                BrandColor = owner.BrandColor,
                ReminderHoursBefore = owner.ReminderHoursBefore,
                DepositEnabled = owner.DepositEnabled,
                DepositPercent = owner.DepositPercent,
                MasterFee = owner.MasterFee,
                WalletAddress = owner.WalletAddress,
                IsSubscribed = owner.IsSubscribed,
                Status = owner.Status.ToString()
            };

            return Ok(response);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound("Owner not found.");

            owner.BarbershopName = request.BarbershopName;
            owner.BarbershopAddress = request.BarbershopAddress;
            owner.BarbershopDescription = request.BarbershopDescription;
            owner.OwnerName = request.OwnerName;
            owner.TimeZone = request.TimeZone;
            owner.LogoUrl = request.LogoUrl;
            owner.BrandColor = request.BrandColor;
            owner.ReminderHoursBefore = request.ReminderHoursBefore;
            owner.DepositEnabled = request.DepositEnabled;
            owner.DepositPercent = request.DepositPercent;
            owner.MasterFee = request.MasterFee;
            owner.WalletAddress = request.WalletAddress;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Settings updated successfully." });
        }
    }
}
