using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        private bool TryGetOwnerId(out Guid ownerId)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            return Guid.TryParse(claim, out ownerId);
        }

        private bool TryGetClientId(out Guid clientId)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "ClientId")?.Value;
            return Guid.TryParse(claim, out clientId);
        }

        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddBarber([FromBody] AddBarberRequest request)
        {
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

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
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var barber = await _context.Masters.FindAsync(request.BarberId);
            if (barber == null || barber.OwnerId != owner.Id)
                return NotFound(new { message = "Barber not found or does not belong to the owner" });

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
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var barber = await _context.Masters.FindAsync(request.BarberId);
            if (barber == null || barber.OwnerId != owner.Id)
                return NotFound(new { message = "Barber not found or does not belong to the owner" });

            _context.Masters.Remove(barber);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Barber deleted successfully" });
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetBarbers()
        {
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var barbers = await _context.Masters.Where(b => b.OwnerId == owner.Id).ToListAsync();
            return Ok(barbers);
        }

        [HttpGet("{barberId}")]
        [Authorize]
        public async Task<IActionResult> GetBarberById([FromRoute] Guid barberId)
        {
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var barber = await _context.Masters.FindAsync(barberId);
            if (barber == null || barber.OwnerId != owner.Id)
                return NotFound(new { message = "Barber not found or does not belong to the owner" });

            return Ok(barber);
        }

        [HttpPost("Add-Shift")]
        [Authorize]
        public async Task<IActionResult> AddShift([FromBody] AddShiftRequest request)
        {
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var barber = await _context.Masters.FindAsync(request.MasterId);
            if (barber == null || barber.OwnerId != owner.Id)
                return NotFound(new { message = "Barber not found or does not belong to the owner" });

            bool overlap = await _context.Shifts.AnyAsync(s =>
                s.MasterId == barber.Id &&
                s.Date == request.Date &&
                s.StartTime < request.EndTime &&
                s.EndTime > request.StartTime);
            if (overlap)
                return Conflict(new { message = "Shift overlaps with an existing shift" });

            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                MasterId = barber.Id,
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Shift added successfully", shiftId = shift.Id });
        }

        [HttpGet("Get-Shift/{shiftId}")]
        [Authorize]
        public async Task<IActionResult> GetShift([FromRoute] Guid shiftId)
        {
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var shift = await _context.Shifts.Include(s => s.Master).FirstOrDefaultAsync(s => s.Id == shiftId);
            if (shift == null || shift.Master.OwnerId != owner.Id)
                return NotFound(new { message = "Shift not found" });

            return Ok(shift);
        }

        [HttpPut("Update-Shift/{shiftId}")]
        [Authorize]
        public async Task<IActionResult> UpdateShift([FromRoute] Guid shiftId, [FromBody] AddShiftRequest request)
        {
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var shift = await _context.Shifts.Include(s => s.Master).FirstOrDefaultAsync(s => s.Id == shiftId);
            if (shift == null || shift.Master.OwnerId != owner.Id)
                return NotFound(new { message = "Shift not found" });

            bool overlap = await _context.Shifts.AnyAsync(s =>
                s.Id != shiftId &&
                s.MasterId == shift.MasterId &&
                s.Date == request.Date &&
                s.StartTime < request.EndTime &&
                s.EndTime > request.StartTime);
            if (overlap)
                return Conflict(new { message = "Shift overlaps with an existing shift" });

            shift.Date = request.Date;
            shift.StartTime = request.StartTime;
            shift.EndTime = request.EndTime;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Shift updated successfully" });
        }

        [HttpDelete("Delete-Shift/{shiftId}")]
        [Authorize]
        public async Task<IActionResult> DeleteShift([FromRoute] Guid shiftId)
        {
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var shift = await _context.Shifts.Include(s => s.Master).FirstOrDefaultAsync(s => s.Id == shiftId);
            if (shift == null || shift.Master.OwnerId != owner.Id)
                return NotFound(new { message = "Shift not found" });

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Shift deleted successfully" });
        }

        [HttpGet("Get-Review/{reviewId}")]
        [Authorize]
        public async Task<IActionResult> GetReview([FromRoute] Guid reviewId)
        {
            if (!TryGetOwnerId(out var ownerId))
                return Unauthorized();

            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var review = await _context.Reviews.Include(r => r.Master).FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review == null || review.Master.OwnerId != owner.Id)
                return NotFound(new { message = "Review not found" });

            return Ok(review);
        }

        [HttpPost("Add-Review")]
        [Authorize]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest request)
        {
            if (!TryGetClientId(out var clientId))
                return Unauthorized();

            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                return NotFound(new { message = "Client not found" });

            var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
            if (appointment == null || appointment.ClientId != clientId || appointment.MasterId != request.MasterId)
                return NotFound(new { message = "Appointment not found" });

            bool alreadyReviewed = await _context.Reviews.AnyAsync(r => r.AppointmentId == request.AppointmentId);
            if (alreadyReviewed)
                return Conflict(new { message = "Appointment already reviewed" });

            var review = new Review
            {
                Id = Guid.NewGuid(),
                MasterId = request.MasterId,
                ClientId = clientId,
                AppointmentId = request.AppointmentId,
                Rating = request.Rating,
                Comment = request.Comment
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Review added successfully", reviewId = review.Id });
        }

        public class AddShiftRequest
        {
            public Guid MasterId { get; set; }
            public DateOnly Date { get; set; }
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
        }

        public class AddReviewRequest
        {
            public Guid MasterId { get; set; }
            public Guid AppointmentId { get; set; }
            [Range(1, 5)]
            public int Rating { get; set; }
            [MaxLength(1000)]
            public string? Comment { get; set; }
        }
    }
}