using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly StatisticService _statisticService;

        public StatisticController(AppDbContext context, StatisticService statisticService)
        {
            _context = context;
            _statisticService = statisticService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetTranzactionHistory()
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

            var transactions = await _context.Tranxactions
                .Where(t => t.OwnerId == owner.Id)
                .OrderByDescending(t => t.Time)
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpGet("owner-statistic-daily")]
        [Authorize]
        public async Task<IActionResult> GetOwnerStatistic([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null) return Unauthorized();

            if (startDate == default || endDate == default)
            {
                return BadRequest("startDate and endDate are required.");
            }

            var stats = await _statisticService.GetOwnerStatisticByDate(Guid.Parse(ownerId), startDate, endDate);
            return Ok(stats);
        }

        [HttpGet("owner-statistic-hourly")]
        [Authorize]
        public async Task<IActionResult> GetOwnerStatisticHourly([FromQuery] DateTime date)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null) return Unauthorized();

            if (date == default) return BadRequest("date is required.");

            var stats = await _statisticService.GetOwnerStatisticByHour(Guid.Parse(ownerId), date);
            return Ok(stats);
        }

        [HttpGet("barber-statistic-daily/{masterId}")]
        [Authorize]
        public async Task<IActionResult> GetBarberStatisticDaily([FromRoute] Guid masterId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null) return Unauthorized();

            if (startDate == default || endDate == default) return BadRequest("startDate and endDate are required.");

            var stats = await _statisticService.GetBarberStatisticByDate(masterId, Guid.Parse(ownerId), startDate, endDate);
            return Ok(stats);
        }

        [HttpGet("barber-statistic-hourly/{masterId}")]
        [Authorize]
        public async Task<IActionResult> GetBarberStatisticHourly([FromRoute] Guid masterId, [FromQuery] DateTime date)
        {
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            if (ownerId == null) return Unauthorized();

            if (date == default) return BadRequest("date is required.");

            var stats = await _statisticService.GetBarberStatisticByHour(masterId, Guid.Parse(ownerId), date);
            return Ok(stats);
        }
    }
}
