using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BarberController : ControllerBase
    {
        private readonly AppDbContext _context;
        public BarberController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddBarber([FromBody] AddBarberRequest request)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(ownerId));
            if (owner == null)
            {
                return NotFound(new { message = "Owner not found" });
            }
            var barber = new Master
            {
                Name = request.Name,
                Description = request.Description,
                TelegramId = request.TelegramId,
                PhotoUrl = request.PhotoUrl,
                OwnerId = owner.Id,
                Ip = HttpContext.Connection.RemoteIpAddress ?? System.Net.IPAddress.Loopback
            };
            _context.Masters.Add(barber);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Barber added successfully", barberId = barber.Id });
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateBarber([FromBody] UpdateBarberRequest request)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(ownerId));
            if (owner == null)
            {
                return NotFound(new { message = "Owner not found" });
            }
            var barber = await _context.Masters.FindAsync(request.BarberId);
            if (barber == null || barber.OwnerId != owner.Id)
            {
                return NotFound(new { message = "Barber not found or does not belong to the owner" });
            }
            barber.Name = request.Name;
            barber.Description = request.Description;
            barber.TelegramId = request.TelegramId;
            barber.PhotoUrl = request.PhotoUrl;
            barber.IsActive = request.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Barber updated successfully" });
        }

        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> DeleteBarber([FromBody] DeleteBarberRequest request)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(ownerId));
            if (owner == null)
            {
                return NotFound(new { message = "Owner not found" });
            }
            var barber = await _context.Masters.FindAsync(request.BarberId);
            if (barber == null || barber.OwnerId != owner.Id)
            {
                return NotFound(new { message = "Barber not found or does not belong to the owner" });
            }
            _context.Masters.Remove(barber);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Barber deleted successfully" });
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetBarbers()
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(ownerId));
            if (owner == null)
            {
                return NotFound(new { message = "Owner not found" });
            }
            var barbers = _context.Masters.Where(b => b.OwnerId == owner.Id).ToList();
            return Ok(barbers);
        }

        [HttpGet("{barberId}")]
        [Authorize]
        public async Task<IActionResult> GetBarberById([FromRoute] Guid barberId)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null)
            {
                return Unauthorized();
            }
            var owner = await _context.BarbershopOwners.FindAsync(Guid.Parse(ownerId));
            if (owner == null)
            {
                return NotFound(new { message = "Owner not found" });
            }
            var barber = await _context.Masters.FindAsync(barberId);
            if (barber == null || barber.OwnerId != owner.Id)
            {
                return NotFound(new { message = "Barber not found or does not belong to the owner" });
            }
            return Ok(barber);
        }
    }
}
