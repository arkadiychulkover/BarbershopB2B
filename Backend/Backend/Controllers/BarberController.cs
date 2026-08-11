using Backend.Data;
using Backend.Models;
using Backend.Models.Enums;
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

        private bool TryGetMasterId(out Guid masterId)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "MasterId")?.Value;
            return Guid.TryParse(claim, out masterId);
        }

        private bool TryGetClientId(out Guid clientId)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "ClientId")?.Value;
            return Guid.TryParse(claim, out clientId);
        }

        private async Task<Guid> GetOrCreateDummyClientAsync(Guid ownerId)
        {
            var dummyClient = await _context.Clients.FirstOrDefaultAsync(c => c.OwnerId == ownerId && c.TelegramId == "WALKIN");
            if (dummyClient == null)
            {
                dummyClient = new Client
                {
                    Id = Guid.NewGuid(),
                    OwnerId = ownerId,
                    Name = "Гость",
                    TelegramId = "WALKIN",
                    Phone = null
                };
                _context.Clients.Add(dummyClient);
                await _context.SaveChangesAsync();
            }
            return dummyClient.Id;
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
                s.DayOfWeek == request.DayOfWeek &&
                s.StartTime < request.EndTime &&
                s.EndTime > request.StartTime);
            if (overlap)
                return Conflict(new { message = "Shift overlaps with an existing shift" });

            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                MasterId = barber.Id,
                DayOfWeek = request.DayOfWeek,
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
                s.DayOfWeek == request.DayOfWeek &&
                s.StartTime < request.EndTime &&
                s.EndTime > request.StartTime);
            if (overlap)
                return Conflict(new { message = "Shift overlaps with an existing shift" });

            shift.DayOfWeek = request.DayOfWeek;
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

        [HttpPost("my-shift/add")]
        [Authorize]
        public async Task<IActionResult> AddMyShift([FromBody] MyShiftRequest request)
        {
            if (!TryGetMasterId(out var masterId))
                return Unauthorized();

            bool overlap = await _context.Shifts.AnyAsync(s =>
                s.MasterId == masterId &&
                s.DayOfWeek == request.DayOfWeek &&
                s.StartTime < request.EndTime &&
                s.EndTime > request.StartTime);
            if (overlap)
                return Conflict(new { message = "Shift overlaps with an existing shift" });

            var shift = new Shift
            {
                Id = Guid.NewGuid(),
                MasterId = masterId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Shift added successfully", shiftId = shift.Id });
        }

        [HttpGet("my-shift/all")]
        [Authorize]
        public async Task<IActionResult> GetMyShifts()
        {
            if (!TryGetMasterId(out var masterId))
                return Unauthorized();

            var shifts = await _context.Shifts.Where(s => s.MasterId == masterId).ToListAsync();
            return Ok(shifts);
        }

        [HttpPut("my-shift/update/{shiftId}")]
        [Authorize]
        public async Task<IActionResult> UpdateMyShift([FromRoute] Guid shiftId, [FromBody] MyShiftRequest request)
        {
            if (!TryGetMasterId(out var masterId))
                return Unauthorized();

            var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId && s.MasterId == masterId);
            if (shift == null)
                return NotFound(new { message = "Shift not found" });

            bool overlap = await _context.Shifts.AnyAsync(s =>
                s.Id != shiftId &&
                s.MasterId == masterId &&
                s.DayOfWeek == request.DayOfWeek &&
                s.StartTime < request.EndTime &&
                s.EndTime > request.StartTime);
            if (overlap)
                return Conflict(new { message = "Shift overlaps with an existing shift" });

            shift.DayOfWeek = request.DayOfWeek;
            shift.StartTime = request.StartTime;
            shift.EndTime = request.EndTime;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Shift updated successfully" });
        }

        [HttpDelete("my-shift/delete/{shiftId}")]
        [Authorize]
        public async Task<IActionResult> DeleteMyShift([FromRoute] Guid shiftId)
        {
            if (!TryGetMasterId(out var masterId))
                return Unauthorized();

            var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId && s.MasterId == masterId);
            if (shift == null)
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

        [HttpGet("my-reviews")]
        [Authorize]
        public async Task<IActionResult> GetMyReviews()
        {
            if (!TryGetMasterId(out var masterId)) return Unauthorized();

            var reviews = await _context.Reviews
                .Include(r => r.Client)
                .Where(r => r.MasterId == masterId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    ClientName = r.Client != null ? r.Client.Name : "Гость"
                })
                .ToListAsync();

            return Ok(reviews);
        }
        [HttpGet("my-services")]
        [Authorize]
        public async Task<IActionResult> GetMyServices()
        {
            if (!TryGetMasterId(out var masterId)) return Unauthorized();

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null) return NotFound(new { message = "Master not found." });

            var serviceNames = await _context.ServiceNames
                .Where(sn => sn.OwnerId == master.OwnerId)
                .ToListAsync();

            var myServices = await _context.Services
                .Where(s => s.MasterId == masterId)
                .ToListAsync();

            var response = serviceNames.Select(sn =>
            {
                var service = myServices.FirstOrDefault(s => s.ServiceNameId == sn.Id);
                return new Backend.DTOs.MasterServiceDto
                {
                    ServiceNameId = sn.Id,
                    Name = sn.Name,
                    ServiceId = service?.Id,
                    Price = service?.Price,
                    Duration = service?.Duration,
                    IsActive = service?.IsActive ?? false,
                    Description = service?.Description
                };
            }).ToList();

            return Ok(response);
        }

        [HttpPut("my-services/{serviceNameId}")]
        [Authorize]
        public async Task<IActionResult> UpdateMyService(Guid serviceNameId, [FromBody] Backend.DTOs.UpdateMasterServiceRequest request)
        {
            if (!TryGetMasterId(out var masterId)) return Unauthorized();

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null) return NotFound(new { message = "Master not found." });

            var serviceName = await _context.ServiceNames.FirstOrDefaultAsync(sn => sn.Id == serviceNameId && sn.OwnerId == master.OwnerId);
            if (serviceName == null) return NotFound(new { message = "Service name not found or doesn't belong to this barbershop." });

            var service = await _context.Services.FirstOrDefaultAsync(s => s.ServiceNameId == serviceNameId && s.MasterId == masterId);

            if (service == null)
            {
                service = new Service
                {
                    Id = Guid.NewGuid(),
                    MasterId = masterId,
                    ServiceNameId = serviceNameId,
                    Price = request.Price,
                    Duration = request.Duration,
                    IsActive = request.IsActive,
                    Description = request.Description
                };
                _context.Services.Add(service);
            }
            else
            {
                service.Price = request.Price;
                service.Duration = request.Duration;
                service.IsActive = request.IsActive;
                service.Description = request.Description;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Service updated successfully." });
        }

        [HttpGet("get-master-appointments")]
        [Authorize]
        public async Task<IActionResult> GetMasterAppointments()
        {
            if (!TryGetMasterId(out var masterId)) return Unauthorized();

            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .ThenInclude(c => c.Owner)
                .Include(a => a.Master)
                .Where(a => a.MasterId == masterId)
                .ToListAsync();

            var response = appointments.Select(a => new Backend.DTOs.AppointmentDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                MasterId = a.MasterId,
                AppointmentDate = a.AppointmentDate,
                AppointmentEndDate = a.AppointmentEndDate,
                Status = a.Status,
                ReminderSent = a.ReminderSent,
                ServiceId = a.ServiceId
            }).ToList();

            return Ok(response);
        }

        private async Task<IActionResult> CreateAppointmentInternal(Guid masterId, BarberAppointmentRequest request)
        {
            var master = await _context.Masters.FindAsync(masterId);
            if (master == null) return NotFound(new { message = "Master not found" });

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.MasterId == masterId);
            if (service == null || !service.IsActive)
                return NotFound(new { message = "Service not found or inactive" });

            Guid finalClientId;
            if (request.ClientId.HasValue && request.ClientId.Value != Guid.Empty)
            {
                var client = await _context.Clients.FindAsync(request.ClientId.Value);
                if (client == null || client.OwnerId != master.OwnerId)
                    return NotFound(new { message = "Client not found or doesn't belong to this shop" });
                finalClientId = client.Id;
            }
            else
            {
                finalClientId = await GetOrCreateDummyClientAsync(master.OwnerId);
            }

            var appointmentEndDateTime = request.AppointmentDate.AddMinutes(service.Duration);
            var startOfDay = DateTime.SpecifyKind(request.AppointmentDate.Date, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            var overlappingAppointments = await _context.Appointments
                .Where(a => a.MasterId == masterId 
                            && a.AppointmentDate >= startOfDay
                            && a.AppointmentDate < endOfDay
                            && a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();

            bool isOverlap = overlappingAppointments.Any(a => 
                request.AppointmentDate < a.AppointmentEndDate && appointmentEndDateTime > a.AppointmentDate);

            if (isOverlap)
                return Conflict(new { message = "The requested time overlaps with another appointment." });

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                MasterId = masterId,
                ClientId = finalClientId,
                ServiceId = request.ServiceId,
                AppointmentDate = request.AppointmentDate,
                AppointmentEndDate = appointmentEndDateTime,
                Status = request.Status,
                MasterProfit = 0,
                OwnerProfit = 0,
                DepositPaid = false
            };
            
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment created successfully", appointmentId = appointment.Id });
        }

        private async Task<IActionResult> UpdateAppointmentInternal(Guid appointmentId, Guid masterId, BarberAppointmentRequest request)
        {
            var appointment = await _context.Appointments.Include(a => a.Master).FirstOrDefaultAsync(a => a.Id == appointmentId && a.MasterId == masterId);
            if (appointment == null) return NotFound(new { message = "Appointment not found" });

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.MasterId == masterId);
            if (service == null || !service.IsActive)
                return NotFound(new { message = "Service not found or inactive" });

            Guid finalClientId;
            if (request.ClientId.HasValue && request.ClientId.Value != Guid.Empty)
            {
                var client = await _context.Clients.FindAsync(request.ClientId.Value);
                if (client == null || client.OwnerId != appointment.Master.OwnerId)
                    return NotFound(new { message = "Client not found or doesn't belong to this shop" });
                finalClientId = client.Id;
            }
            else
            {
                finalClientId = appointment.ClientId; // keep existing if not changing
            }

            var appointmentEndDateTime = request.AppointmentDate.AddMinutes(service.Duration);
            var startOfDay = DateTime.SpecifyKind(request.AppointmentDate.Date, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            var overlappingAppointments = await _context.Appointments
                .Where(a => a.Id != appointmentId
                            && a.MasterId == masterId 
                            && a.AppointmentDate >= startOfDay
                            && a.AppointmentDate < endOfDay
                            && a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();

            bool isOverlap = overlappingAppointments.Any(a => 
                request.AppointmentDate < a.AppointmentEndDate && appointmentEndDateTime > a.AppointmentDate);

            if (isOverlap && request.Status != AppointmentStatus.Cancelled)
                return Conflict(new { message = "The requested time overlaps with another appointment." });

            appointment.ClientId = finalClientId;
            appointment.ServiceId = request.ServiceId;
            appointment.AppointmentDate = request.AppointmentDate;
            appointment.AppointmentEndDate = appointmentEndDateTime;
            appointment.Status = request.Status;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Appointment updated successfully" });
        }

        [HttpPost("my-appointments/add")]
        [Authorize]
        public async Task<IActionResult> AddMyAppointment([FromBody] BarberAppointmentRequest request)
        {
            if (!TryGetMasterId(out var masterId)) return Unauthorized();
            return await CreateAppointmentInternal(masterId, request);
        }

        [HttpPut("my-appointments/update/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateMyAppointment(Guid id, [FromBody] BarberAppointmentRequest request)
        {
            if (!TryGetMasterId(out var masterId)) return Unauthorized();
            return await UpdateAppointmentInternal(id, masterId, request);
        }

        [HttpDelete("my-appointments/delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteMyAppointment(Guid id)
        {
            if (!TryGetMasterId(out var masterId)) return Unauthorized();
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.MasterId == masterId);
            if (appointment == null) return NotFound(new { message = "Appointment not found" });
            
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Appointment deleted successfully" });
        }

        [HttpGet("admin-appointments/all")]
        [Authorize]
        public async Task<IActionResult> GetAdminAppointments()
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();

            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Master)
                .Where(a => a.Master.OwnerId == ownerId)
                .Select(a => new Backend.DTOs.AppointmentDto
                {
                    Id = a.Id,
                    ClientId = a.ClientId,
                    MasterId = a.MasterId,
                    AppointmentDate = a.AppointmentDate,
                    AppointmentEndDate = a.AppointmentEndDate,
                    Status = a.Status,
                    ReminderSent = a.ReminderSent,
                    ServiceId = a.ServiceId
                })
                .ToListAsync();

            return Ok(appointments);
        }

        [HttpPost("admin-appointments/add")]
        [Authorize]
        public async Task<IActionResult> AddAdminAppointment([FromBody] BarberAppointmentRequest request)
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();
            if (!request.MasterId.HasValue) return BadRequest(new { message = "MasterId is required" });

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.Id == request.MasterId.Value && m.OwnerId == ownerId);
            if (master == null) return NotFound(new { message = "Master not found or doesn't belong to your shop" });

            return await CreateAppointmentInternal(master.Id, request);
        }

        [HttpPut("admin-appointments/update/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAdminAppointment(Guid id, [FromBody] BarberAppointmentRequest request)
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();
            
            var appointment = await _context.Appointments.Include(a => a.Master).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null || appointment.Master.OwnerId != ownerId) 
                return NotFound(new { message = "Appointment not found" });

            return await UpdateAppointmentInternal(id, appointment.MasterId, request);
        }

        [HttpDelete("admin-appointments/delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAdminAppointment(Guid id)
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();
            
            var appointment = await _context.Appointments.Include(a => a.Master).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null || appointment.Master.OwnerId != ownerId) 
                return NotFound(new { message = "Appointment not found" });
            
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Appointment deleted successfully" });
        }

        [HttpGet("admin-services/{masterId}")]
        [Authorize]
        public async Task<IActionResult> GetAdminServices(Guid masterId)
        {
            if (!TryGetOwnerId(out var ownerId)) return Unauthorized();

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.Id == masterId && m.OwnerId == ownerId);
            if (master == null) return NotFound(new { message = "Master not found" });

            var serviceNames = await _context.ServiceNames.Where(sn => sn.OwnerId == ownerId).ToListAsync();
            var myServices = await _context.Services.Where(s => s.MasterId == masterId).ToListAsync();

            var response = serviceNames.Select(sn =>
            {
                var service = myServices.FirstOrDefault(s => s.ServiceNameId == sn.Id);
                return new Backend.DTOs.MasterServiceDto
                {
                    ServiceNameId = sn.Id,
                    Name = sn.Name,
                    ServiceId = service?.Id,
                    Price = service?.Price,
                    Duration = service?.Duration,
                    IsActive = service?.IsActive ?? false,
                    Description = service?.Description
                };
            }).Where(s => s.ServiceId != null && s.IsActive).ToList();

            return Ok(response);
        }

        public class AddShiftRequest
        {
            public Guid MasterId { get; set; }
            public DayOfWeek DayOfWeek { get; set; }
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
        }

        public class MyShiftRequest
        {
            public DayOfWeek DayOfWeek { get; set; }
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

        public class BarberAppointmentRequest
        {
            public Guid? MasterId { get; set; }
            public Guid? ClientId { get; set; }
            public Guid ServiceId { get; set; }
            public DateTime AppointmentDate { get; set; }
            public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
        }
    }
}