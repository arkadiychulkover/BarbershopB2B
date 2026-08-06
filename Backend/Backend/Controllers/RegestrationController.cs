using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegestrationController : ControllerBase
    {
        private readonly AppDbContext _context;
        public RegestrationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            HMACSHA256 hmac = new HMACSHA256();
            string passwordHash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password)));
            _context.BarbershopOwners.Add(new BarbershopOwner
            {
                OwnerName = request.OwnerName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                TelegramId = request.TelegramId,
                BarbershopName = request.BarbershopName,
                BarbershopAddress = request.BarbershopAddress,
                BarbershopDescription = request.BarbershopDescription,
                BotToken = request.BotToken,
                BotUsername = request.BotUsername,
                TimeZone = request.TimeZone,
                PasswordHash = passwordHash,
            });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Registration successful" });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(ownerId));
            if (owner == null)
            {
                return NotFound();
            }
            owner.LogoUrl = request.LogoUrl;
            owner.BrandColor = request.BrandColor;
            owner.ReminderHoursBefore = request.ReminderHoursBefore;
            owner.DepositEnabled = request.DepositEnabled;
            owner.DepositPercent = request.DepositPercent;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Settings updated successfully" });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(ownerId));
            if (owner == null)
            {
                return NotFound();
            }
            HMACSHA256 hmac = new HMACSHA256();
            string currentPasswordHash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.CurrentPassword)));
            if (owner.PasswordHash != currentPasswordHash)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }
            string newPasswordHash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.NewPassword)));
            owner.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password changed successfully" });
        }

        //[HttpPost]
        //[Authorize]
        //public async Task<IActionResult> Logout()
        //{

        //}

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSettings()
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(ownerId));
            if (owner == null)
            {
                return NotFound();
            }
            var settings = new
            {
                owner.LogoUrl,
                owner.BrandColor,
                owner.ReminderHoursBefore,
                owner.DepositEnabled,
                owner.DepositPercent
            };
            return Ok(settings);
        }
    }

    public class UpdateSettingsRequest
    {
        [Required]
        public string LogoUrl { get; set; }
        [Required]
        public string BrandColor { get; set; }
        [Required]
        public int ReminderHoursBefore { get; set; }
        [Required]
        public bool DepositEnabled { get; set; }
        [Required]
        public int DepositPercent { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; }
        [Required]
        public string NewPassword { get; set; }
    }
}
