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
    [Authorize]
    public class StatisticController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly StatisticService _statisticService;
        private readonly ExcelExportService _excelExportService;

        public StatisticController(
            AppDbContext context, 
            StatisticService statisticService,
            ExcelExportService excelExportService)
        {
            _context = context;
            _statisticService = statisticService;
            _excelExportService = excelExportService;
        }

        [HttpGet]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetTranzactionHistory()
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Доступ к истории транзакций ограничен." });

            var transactions = await _context.Tranxactions
                .AsNoTracking()
                .Where(t => t.OwnerId == owner.Id)
                .OrderByDescending(t => t.Time)
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpGet("owner-statistic-daily")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetOwnerStatistic([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Доступ к аналитике ограничен." });

            if (startDate == default || endDate == default)
                return BadRequest("startDate and endDate are required.");

            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var stats = await _statisticService.GetOwnerStatisticByDate(ownerId, startDate, endDate);
            return Ok(stats);
        }

        [HttpGet("owner-statistic-hourly")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetOwnerStatisticHourly([FromQuery] DateTime date)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Доступ к аналитике ограничен." });

            if (date == default) return BadRequest("date is required.");

            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            var stats = await _statisticService.GetOwnerStatisticByHour(ownerId, date);
            return Ok(stats);
        }

        [HttpGet("barber-statistic-daily/{masterId}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetBarberStatisticDaily([FromRoute] Guid masterId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Доступ к аналитике ограничен." });

            if (startDate == default || endDate == default) return BadRequest("startDate and endDate are required.");

            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var stats = await _statisticService.GetBarberStatisticByDate(masterId, ownerId, startDate, endDate);
            return Ok(stats);
        }

        [HttpGet("barber-statistic-hourly/{masterId}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetBarberStatisticHourly([FromRoute] Guid masterId, [FromQuery] DateTime date)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Доступ к аналитике ограничен." });

            if (date == default) return BadRequest("date is required.");

            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            var stats = await _statisticService.GetBarberStatisticByHour(masterId, ownerId, date);
            return Ok(stats);
        }

        [HttpGet("owner-chart-analytics")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetOwnerChartAnalytics(
            [FromQuery] string? period = "30d", 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Доступ к аналитике ограничен." });

            var analytics = await _statisticService.GetChartAnalyticsAsync(ownerId, period ?? "30d", startDate, endDate);
            return Ok(analytics);
        }

        [HttpGet("admin/chart-analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminChartAnalytics(
            [FromQuery] Guid? ownerId, 
            [FromQuery] string? period = "30d", 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            var analytics = await _statisticService.GetChartAnalyticsAsync(ownerId, period ?? "30d", startDate, endDate);
            return Ok(analytics);
        }

        [HttpGet("owner-clients")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetOwnerClients([FromQuery] string? search = null)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Доступ к базе клиентов ограничен." });

            var query = _context.Clients
                .AsNoTracking()
                .Where(c => c.OwnerId == ownerId && c.TelegramId != "WALKIN")
                .Include(c => c.Appointments)
                    .ThenInclude(a => a.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim().ToLower();
                query = query.Where(c => (c.Name != null && c.Name.ToLower().Contains(cleanSearch))
                                      || (c.Phone != null && c.Phone.Contains(cleanSearch))
                                      || c.TelegramId.Contains(cleanSearch));
            }

            var clients = await query
                .OrderByDescending(c => c.Appointments.Count)
                .ToListAsync();

            var dtos = clients.Select(c =>
            {
                var validAppointments = c.Appointments
                    .Where(a => a.Status == Models.Enums.AppointmentStatus.Completed)
                    .OrderBy(a => a.AppointmentDate)
                    .ToList();

                var totalSpent = validAppointments.Sum(a => a.Service != null ? a.Service.Price : (a.OwnerProfit + a.MasterProfit));
                var lastVisit = validAppointments.LastOrDefault()?.AppointmentDate;
                var firstVisit = validAppointments.FirstOrDefault()?.AppointmentDate;

                return new
                {
                    c.Id,
                    c.Name,
                    Phone = c.Phone,
                    c.TelegramId,
                    c.Notes,
                    c.IsBlacklisted,
                    TotalVisits = c.Appointments.Count(a => a.Status != Models.Enums.AppointmentStatus.Cancelled),
                    TotalSpent = totalSpent,
                    FirstVisit = firstVisit,
                    LastVisit = lastVisit
                };
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("export-clients")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ExportOwnerClients()
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Экспорт клиентов ограничен." });

            var fileBytes = await _excelExportService.ExportOwnerClientsToExcelAsync(owner.Id, owner.BarbershopName);
            var fileName = $"clients_{owner.BarbershopName.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        [HttpGet("admin/export-clients")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportAdminClients([FromQuery] Guid? ownerId)
        {
            var fileBytes = await _excelExportService.ExportAllClientsToExcelAsync(ownerId);
            var fileName = $"platform_clients_{DateTime.UtcNow:yyyyMMdd}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
