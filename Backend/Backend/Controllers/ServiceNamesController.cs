using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
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
        private readonly BotService? _botService;

        public ServiceNamesController(AppDbContext context, BotService? botService = null)
        {
            _context = context;
            _botService = botService;
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

            var masterServices = await _context.Services
                .Where(s => s.ServiceNameId == id)
                .ToListAsync();
            var masterServiceIds = masterServices.Select(s => s.Id).ToList();

            if (masterServiceIds.Any())
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.AdditionalServices)
                    .Where(a => masterServiceIds.Contains(a.ServiceId) 
                             || a.AdditionalServices.Any(ads => masterServiceIds.Contains(ads.ServiceId)))
                    .ToListAsync();

                if (_botService != null && !string.IsNullOrEmpty(owner.BotToken))
                {
                    var now = DateTime.UtcNow;
                    var activeAppointments = appointments
                        .Where(a => a.Status == AppointmentStatus.Scheduled && a.AppointmentDate >= now)
                        .ToList();

                    var messagesToSend = new Dictionary<long, string>();
                    foreach (var appt in activeAppointments)
                    {
                        if (appt.Client != null 
                            && !string.IsNullOrEmpty(appt.Client.TelegramId) 
                            && appt.Client.TelegramId != "WALKIN"
                            && !appt.Client.HasBlocked
                            && long.TryParse(appt.Client.TelegramId, out long clientChatId))
                        {
                            var dateStr = appt.AppointmentDate.ToString("dd.MM в HH:mm");
                            messagesToSend[clientChatId] = $"Ваша запись на услугу «{serviceName.Name}» ({dateStr}) отменена в связи с удалением услуги заведением. Приносим извинения за неудобства.";
                        }
                    }

                    if (messagesToSend.Count > 0)
                    {
                        await _botService.SendBatchMessagesAsync(owner.BotToken, messagesToSend, HttpContext.RequestAborted);
                    }
                }

                var apptIds = appointments.Select(a => a.Id).ToList();
                var additionalServices = await _context.AppointmentServices
                    .Where(ads => apptIds.Contains(ads.AppointmentId) || masterServiceIds.Contains(ads.ServiceId))
                    .ToListAsync();
                if (additionalServices.Any())
                {
                    _context.AppointmentServices.RemoveRange(additionalServices);
                }

                if (appointments.Any())
                {
                    _context.Appointments.RemoveRange(appointments);
                }

                _context.Services.RemoveRange(masterServices);
            }

            _context.ServiceNames.Remove(serviceName);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Service name and related appointments deleted successfully." });
        }
    }
}
