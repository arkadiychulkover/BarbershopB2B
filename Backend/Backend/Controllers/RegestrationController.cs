using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            bool emailExists = await _context.BarbershopOwners.AnyAsync(o => o.Email.ToLower() == cleanEmail);
            if (emailExists)
            {
                return BadRequest(new { message = "Пользователь с таким email уже зарегистрирован." });
            }

            string passwordHash = PasswordSecurity.HashPassword(request.Password);

            _context.BarbershopOwners.Add(new BarbershopOwner
            {
                OwnerName = request.OwnerName?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
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
}
