using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceNamesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServiceNamesController(AppDbContext context)
        {
            _context = context;
        }

        private bool TryGetOwnerId(out Guid ownerId)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "OwnerId")?.Value;
            return Guid.TryParse(claim, out ownerId);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceNames()
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();

            var serviceNames = await _context.ServiceNames
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
        [Authorize]
        public async Task<IActionResult> CreateServiceName([FromBody] CreateServiceNameRequest request)
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Service name is required." });

            var serviceName = new ServiceName
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
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
        [Authorize]
        public async Task<IActionResult> UpdateServiceName(Guid id, [FromBody] UpdateServiceNameRequest request)
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();

            var serviceName = await _context.ServiceNames.FirstOrDefaultAsync(sn => sn.Id == id && sn.OwnerId == ownerId);
            if (serviceName == null) return NotFound(new { message = "Service name not found." });

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Service name is required." });

            serviceName.Name = request.Name;
            await _context.SaveChangesAsync();

            return Ok(new ServiceNameDto
            {
                Id = serviceName.Id,
                Name = serviceName.Name
            });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteServiceName(Guid id)
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();

            var serviceName = await _context.ServiceNames.FirstOrDefaultAsync(sn => sn.Id == id && sn.OwnerId == ownerId);
            if (serviceName == null) return NotFound(new { message = "Service name not found." });

            _context.ServiceNames.Remove(serviceName);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Service name deleted successfully." });
        }
    }
}
