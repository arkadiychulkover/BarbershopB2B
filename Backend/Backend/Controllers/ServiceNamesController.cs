using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Owner")]
    public class ServiceNamesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServiceNamesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceNames()
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound("Owner not found.");

            var serviceNames = await _context.ServiceNames
                .AsNoTracking()
                .Where(sn => sn.OwnerId == ownerId)
                .Select(sn => new ServiceNameDto
                {
                    Id = sn.Id,
                    Name = sn.Name
                })
                .ToListAsync();

            return Ok(serviceNames);
        }

        [HttpPost]
        public async Task<IActionResult> CreateServiceName([FromBody] CreateServiceNameRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound("Owner not found.");

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Оплатите тариф для добавления услуг." });

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Service name is required." });

            var serviceName = new ServiceName
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                OwnerId = ownerId
            };

            _context.ServiceNames.Add(serviceName);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetServiceNames), new { id = serviceName.Id }, new ServiceNameDto
            {
                Id = serviceName.Id,
                Name = serviceName.Name
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateServiceName(Guid id, [FromBody] UpdateServiceNameRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound("Owner not found.");

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Оплатите тариф для изменения услуг." });

            var serviceName = await _context.ServiceNames.FirstOrDefaultAsync(sn => sn.Id == id && sn.OwnerId == ownerId);
            if (serviceName == null) return NotFound(new { message = "Service name not found." });

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Service name is required." });

            serviceName.Name = request.Name.Trim();
            await _context.SaveChangesAsync();

            return Ok(new ServiceNameDto
            {
                Id = serviceName.Id,
                Name = serviceName.Name
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteServiceName(Guid id)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound("Owner not found.");

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка не активна. Оплатите тариф для удаления услуг." });

            var serviceName = await _context.ServiceNames.FirstOrDefaultAsync(sn => sn.Id == id && sn.OwnerId == ownerId);
            if (serviceName == null) return NotFound(new { message = "Service name not found." });

            _context.ServiceNames.Remove(serviceName);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Service name deleted successfully." });
        }
    }
}
