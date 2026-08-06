using Backend.Data;
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

        public StatisticController(AppDbContext context)
        {
            _context = context;
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
    }
}
