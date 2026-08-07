using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegestrationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        public RegestrationController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var sha256 = SHA256.Create();
            string passwordHash = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password)));
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
                WalletAddress = ""
            });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Registration successful" });
        }

        [HttpPost("change-password")]
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
            using var sha256 = SHA256.Create();
            string currentPasswordHash = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.CurrentPassword)));
            if (owner.PasswordHash != currentPasswordHash)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }
            string newPasswordHash = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.NewPassword)));
            owner.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var owner = await _context.BarbershopOwners.FirstOrDefaultAsync(o => o.Email == request.Email);
            if (owner == null) return Unauthorized(new { message = "Invalid email or password" });

            using var sha256 = SHA256.Create();
            string passwordHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(request.Password)));

            if (owner.PasswordHash != passwordHash) return Unauthorized(new { message = "Invalid email or password" });

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("Secret");

            var claims = new[]
            {
                new Claim("OwnerId", owner.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, owner.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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
                expiration = token.ValidTo
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
}
