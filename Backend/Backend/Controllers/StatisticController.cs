using Backend.Data;
using Backend.Extensions;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Owner")]
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
        public async Task<IActionResult> GetTranzactionHistory()
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var transactions = await _context.Tranxactions
                .Where(t => t.OwnerId == owner.Id)
                .OrderByDescending(t => t.Time)
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpGet("owner-statistic-daily")]
        public async Task<IActionResult> GetOwnerStatistic([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var ownerId = User.GetUserId();

            if (startDate == default || endDate == default)
                return BadRequest("startDate and endDate are required.");

            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var stats = await _statisticService.GetOwnerStatisticByDate(ownerId, startDate, endDate);
            return Ok(stats);
        }

        [HttpGet("owner-statistic-hourly")]
        public async Task<IActionResult> GetOwnerStatisticHourly([FromQuery] DateTime date)
        {
            var ownerId = User.GetUserId();

            if (date == default) return BadRequest("date is required.");

            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            var stats = await _statisticService.GetOwnerStatisticByHour(ownerId, date);
            return Ok(stats);
        }

        [HttpGet("barber-statistic-daily/{masterId}")]
        public async Task<IActionResult> GetBarberStatisticDaily([FromRoute] Guid masterId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var ownerId = User.GetUserId();

            if (startDate == default || endDate == default) return BadRequest("startDate and endDate are required.");

            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var stats = await _statisticService.GetBarberStatisticByDate(masterId, ownerId, startDate, endDate);
            return Ok(stats);
        }

        [HttpGet("barber-statistic-hourly/{masterId}")]
        public async Task<IActionResult> GetBarberStatisticHourly([FromRoute] Guid masterId, [FromQuery] DateTime date)
        {
            var ownerId = User.GetUserId();

            if (date == default) return BadRequest("date is required.");

            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            var stats = await _statisticService.GetBarberStatisticByHour(masterId, ownerId, date);
            return Ok(stats);
        }
    }
}
