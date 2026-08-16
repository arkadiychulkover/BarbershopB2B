using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Enums;
using Microsoft.AspNetCore.Authorization;
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

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public BarberController(AppDbContext context)
        {
            _context = context;
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

        // ─── Owner: Barber Management ──────────────────────────────────────────────

        [HttpPost("add")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> AddBarber([FromBody] AddBarberRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Оплатите тариф для добавления мастеров." });

            var barber = new Master
            {
                Name = request.Name,
                Description = request.Description,
                TelegramId = request.TelegramId,
                TelegramUsername = !string.IsNullOrWhiteSpace(request.TelegramUsername) ? request.TelegramUsername.Trim().TrimStart('@') : null,
                PhotoUrl = request.PhotoUrl,
                OwnerId = owner.Id,
                Ip = HttpContext.Connection.RemoteIpAddress ?? System.Net.IPAddress.Loopback
            };
            _context.Masters.Add(barber);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Barber added successfully", barberId = barber.Id });
        }

        [HttpPut("update")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateBarber([FromBody] UpdateBarberRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var barber = await _context.Masters.FindAsync(request.BarberId);
            if (barber == null || barber.OwnerId != owner.Id)
                return NotFound(new { message = "Barber not found or does not belong to the owner" });

            barber.Name = request.Name;
            barber.Description = request.Description;
            barber.TelegramId = request.TelegramId;
            if (request.TelegramUsername != null)
            {
                barber.TelegramUsername = !string.IsNullOrWhiteSpace(request.TelegramUsername) ? request.TelegramUsername.Trim().TrimStart('@') : null;
            }
            barber.PhotoUrl = request.PhotoUrl;
            barber.IsActive = request.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Barber updated successfully" });
        }

        [HttpDelete("delete")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteBarber([FromBody] DeleteBarberRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var barber = await _context.Masters.FindAsync(request.BarberId);
            if (barber == null || barber.OwnerId != owner.Id)
                return NotFound(new { message = "Barber not found or does not belong to the owner" });

            _context.Masters.Remove(barber);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Barber deleted successfully" });
        }

        [HttpGet("all")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetBarbers()
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var barbers = await _context.Masters.AsNoTracking().Where(b => b.OwnerId == owner.Id).ToListAsync();
            return Ok(barbers);
        }

        [HttpGet("{barberId}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetBarberById([FromRoute] Guid barberId)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var barber = await _context.Masters.AsNoTracking().FirstOrDefaultAsync(m => m.Id == barberId && m.OwnerId == owner.Id);
            if (barber == null)
                return NotFound(new { message = "Barber not found or does not belong to the owner" });

            return Ok(barber);
        }

        // ─── Owner: Shift Management ───────────────────────────────────────────────

        [HttpPost("Add-Shift")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> AddShift([FromBody] AddShiftRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

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
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetShift([FromRoute] Guid shiftId)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var shift = await _context.Shifts.AsNoTracking().Include(s => s.Master).FirstOrDefaultAsync(s => s.Id == shiftId);
            if (shift == null || shift.Master.OwnerId != owner.Id)
                return NotFound(new { message = "Shift not found" });

            return Ok(shift);
        }

        [HttpPut("Update-Shift/{shiftId}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateShift([FromRoute] Guid shiftId, [FromBody] AddShiftRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

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
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteShift([FromRoute] Guid shiftId)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var shift = await _context.Shifts.Include(s => s.Master).FirstOrDefaultAsync(s => s.Id == shiftId);
            if (shift == null || shift.Master.OwnerId != owner.Id)
                return NotFound(new { message = "Shift not found" });

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Shift deleted successfully" });
        }

        // ─── Master: Shift Management ──────────────────────────────────────────────

        [HttpPost("my-shift/add")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> AddMyShift([FromBody] MyShiftRequest request)
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == masterId);
            if (master?.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Действие заблокировано." });

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
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> GetMyShifts()
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).AsNoTracking().FirstOrDefaultAsync(m => m.Id == masterId);
            if (master?.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var shifts = await _context.Shifts.AsNoTracking().Where(s => s.MasterId == masterId).ToListAsync();
            return Ok(shifts);
        }

        [HttpPut("my-shift/update/{shiftId}")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> UpdateMyShift([FromRoute] Guid shiftId, [FromBody] MyShiftRequest request)
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == masterId);
            if (master?.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Действие заблокировано." });

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
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> DeleteMyShift([FromRoute] Guid shiftId)
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == masterId);
            if (master?.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Действие заблокировано." });

            var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId && s.MasterId == masterId);
            if (shift == null)
                return NotFound(new { message = "Shift not found" });

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Shift deleted successfully" });
        }

        // ─── Owner: Reviews ────────────────────────────────────────────────────────

        [HttpGet("Get-Review/{reviewId}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetReview([FromRoute] Guid reviewId)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null)
                return NotFound(new { message = "Owner not found" });

            var review = await _context.Reviews.AsNoTracking().Include(r => r.Master).FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review == null || review.Master.OwnerId != owner.Id)
                return NotFound(new { message = "Review not found" });

            return Ok(review);
        }

        [HttpPost("Add-Review")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest request)
        {
            var clientId = User.GetUserId();

            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                return NotFound(new { message = "Client not found" });

            var master = await _context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == request.MasterId);
            if (master?.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

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

        // ─── Master: Profile & Username Management ─────────────────────────────────

        [HttpGet("my-profile")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> GetMyProfile()
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).AsNoTracking().FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null)
                return NotFound(new { message = "Master not found" });

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var dto = new MasterProfileDto
            {
                Id = master.Id,
                Name = master.Name,
                Description = master.Description,
                TelegramId = master.TelegramId,
                TelegramUsername = master.TelegramUsername,
                PhotoUrl = master.PhotoUrl,
                Rating = master.Rating,
                ReviewsCount = master.ReviewsCount,
                IsActive = master.IsActive
            };

            return Ok(dto);
        }

        [HttpPut("my-username")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> UpdateMyUsername([FromBody] UpdateMasterUsernameRequest request)
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null)
                return NotFound(new { message = "Master not found" });

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            string? cleanUsername = null;
            if (!string.IsNullOrWhiteSpace(request.TelegramUsername))
            {
                cleanUsername = request.TelegramUsername.Trim().TrimStart('@');
                if (cleanUsername.Length > 64)
                {
                    return BadRequest(new { message = "Имя пользователя слишком длинное." });
                }
            }

            master.TelegramUsername = cleanUsername;
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = "Имя пользователя успешно обновлено", 
                telegramUsername = master.TelegramUsername 
            });
        }

        [HttpPut("my-profile")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMasterProfileRequest request)
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null)
                return NotFound(new { message = "Master not found" });

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                master.Name = request.Name.Trim();
            }

            if (request.Description != null)
            {
                master.Description = request.Description.Trim();
            }

            if (request.TelegramUsername != null)
            {
                master.TelegramUsername = !string.IsNullOrWhiteSpace(request.TelegramUsername)
                    ? request.TelegramUsername.Trim().TrimStart('@')
                    : null;
            }

            if (request.PhotoUrl != null)
            {
                master.PhotoUrl = request.PhotoUrl;
            }

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = "Профиль успешно обновлен",
                profile = new MasterProfileDto
                {
                    Id = master.Id,
                    Name = master.Name,
                    Description = master.Description,
                    TelegramId = master.TelegramId,
                    TelegramUsername = master.TelegramUsername,
                    PhotoUrl = master.PhotoUrl,
                    Rating = master.Rating,
                    ReviewsCount = master.ReviewsCount,
                    IsActive = master.IsActive
                }
            });
        }

        // ─── Master: Reviews & Services ────────────────────────────────────────────

        [HttpGet("my-reviews")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> GetMyReviews()
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).AsNoTracking().FirstOrDefaultAsync(m => m.Id == masterId);
            if (master?.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Client)
                .Where(r => r.MasterId == masterId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
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
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> GetMyServices()
        {
            var masterId = User.GetUserId();

            var master = await _context.Masters.Include(m => m.Owner).AsNoTracking().FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null) return NotFound(new { message = "Master not found." });

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var serviceNames = await _context.ServiceNames
                .AsNoTracking()
                .Where(sn => sn.OwnerId == master.OwnerId)
                .ToListAsync();

            var myServices = await _context.Services
                .AsNoTracking()
                .Where(s => s.MasterId == masterId)
                .ToListAsync();

            var response = serviceNames.Select(sn =>
            {
                var service = myServices.FirstOrDefault(s => s.ServiceNameId == sn.Id);
                return new MasterServiceDto
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
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> UpdateMyService(Guid serviceNameId, [FromBody] UpdateMasterServiceRequest request)
        {
            var masterId = User.GetUserId();

            var master = await _context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null) return NotFound(new { message = "Master not found." });

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Действие заблокировано." });

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

        // ─── Master: Appointments ──────────────────────────────────────────────────

        [HttpGet("get-master-appointments")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> GetMasterAppointments()
        {
            var masterId = User.GetUserId();

            var master = await _context.Masters.Include(m => m.Owner).AsNoTracking().FirstOrDefaultAsync(m => m.Id == masterId);
            if (master?.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Include(a => a.Service)
                    .ThenInclude(s => s.ServiceName)
                .Include(a => a.Master)
                .Where(a => a.MasterId == masterId)
                .ToListAsync();

            var response = appointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                ClientName = a.Client?.Name,
                ClientTelegramId = a.Client?.TelegramId,
                MasterId = a.MasterId,
                AppointmentDate = a.AppointmentDate,
                AppointmentEndDate = a.AppointmentEndDate,
                Status = a.Status,
                ReminderSent = a.ReminderSent,
                ServiceId = a.ServiceId,
                ServiceName = a.Service?.ServiceName?.Name,
                PhotoResultUrl = a.PhotoResultUrl,
                ResultNote = a.ResultNote
            }).ToList();

            return Ok(response);
        }

        private async Task<IActionResult> CreateAppointmentInternal(Guid masterId, BarberAppointmentRequest request)
        {
            var master = await _context.Masters.Include(m => m.Owner).FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null) return NotFound(new { message = "Master not found" });

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Создание записей заблокировано." });

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
                DepositPaid = false,
                ResultNote = request.Comment
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Appointment created successfully", appointmentId = appointment.Id });
        }

        private async Task<IActionResult> UpdateAppointmentInternal(Guid appointmentId, Guid masterId, BarberAppointmentRequest request)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.MasterId == masterId);
            if (appointment == null) return NotFound(new { message = "Appointment not found" });

            if (appointment.Master?.Owner == null || !appointment.Master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Изменение записей заблокировано." });

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
                finalClientId = appointment.ClientId;
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
            
            if (request.Comment != null)
            {
                appointment.ResultNote = request.Comment;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Appointment updated successfully" });
        }

        [HttpPost("my-appointments/add")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> AddMyAppointment([FromBody] BarberAppointmentRequest request)
        {
            var masterId = User.GetUserId();
            return await CreateAppointmentInternal(masterId, request);
        }

        [HttpGet("client-history/{clientId}")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> GetClientHistory(Guid clientId)
        {
            var masterId = User.GetUserId();
            var master = await _context.Masters.Include(m => m.Owner).AsNoTracking().FirstOrDefaultAsync(m => m.Id == masterId);
            if (master == null) return NotFound(new { message = "Master not found" });

            if (master.Owner == null || !master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            // Ensure client belongs to same barbershop
            var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId && c.OwnerId == master.OwnerId);
            if (client == null) return NotFound(new { message = "Client not found" });

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                    .ThenInclude(s => s.ServiceName)
                .Include(a => a.Master)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    ClientId = a.ClientId,
                    ClientName = client.Name,
                    ClientTelegramId = client.TelegramId,
                    MasterId = a.MasterId,
                    AppointmentDate = a.AppointmentDate,
                    AppointmentEndDate = a.AppointmentEndDate,
                    Status = a.Status,
                    ReminderSent = a.ReminderSent,
                    ServiceId = a.ServiceId,
                    ServiceName = a.Service != null ? a.Service.ServiceName.Name : null,
                    PhotoResultUrl = a.PhotoResultUrl,
                    ResultNote = a.ResultNote
                })
                .ToListAsync();

            return Ok(new
            {
                client = new { client.Id, client.Name, client.TelegramId, client.Phone, client.Notes },
                appointments
            });
        }

        [HttpPut("my-appointments/update/{id}")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> UpdateMyAppointment(Guid id, [FromBody] BarberAppointmentRequest request)
        {
            var masterId = User.GetUserId();
            return await UpdateAppointmentInternal(id, masterId, request);
        }

        [HttpDelete("my-appointments/delete/{id}")]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> DeleteMyAppointment(Guid id)
        {
            var masterId = User.GetUserId();
            var appointment = await _context.Appointments
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(a => a.Id == id && a.MasterId == masterId);
            if (appointment == null) return NotFound(new { message = "Appointment not found" });

            if (appointment.Master?.Owner == null || !appointment.Master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Действие заблокировано." });

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Appointment deleted successfully" });
        }

        // ─── Owner: Appointments ───────────────────────────────────────────────────

        [HttpGet("admin-appointments/all")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetAdminAppointments()
        {
            var ownerId = User.GetUserId();

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Include(a => a.Master)
                .Where(a => a.Master.OwnerId == ownerId)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    ClientId = a.ClientId,
                    MasterId = a.MasterId,
                    AppointmentDate = a.AppointmentDate,
                    AppointmentEndDate = a.AppointmentEndDate,
                    Status = a.Status,
                    ReminderSent = a.ReminderSent,
                    ServiceId = a.ServiceId,
                    PhotoResultUrl = a.PhotoResultUrl,
                    ResultNote = a.ResultNote
                })
                .ToListAsync();

            return Ok(appointments);
        }

        [HttpPost("admin-appointments/add")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> AddAdminAppointment([FromBody] BarberAppointmentRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Создание записей заблокировано." });

            if (!request.MasterId.HasValue) return BadRequest(new { message = "MasterId is required" });

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.Id == request.MasterId.Value && m.OwnerId == ownerId);
            if (master == null) return NotFound(new { message = "Master not found or doesn't belong to your shop" });

            return await CreateAppointmentInternal(master.Id, request);
        }

        [HttpPut("admin-appointments/update/{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateAdminAppointment(Guid id, [FromBody] BarberAppointmentRequest request)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Изменение записей заблокировано." });

            var appointment = await _context.Appointments.Include(a => a.Master).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null || appointment.Master.OwnerId != ownerId)
                return NotFound(new { message = "Appointment not found" });

            return await UpdateAppointmentInternal(id, appointment.MasterId, request);
        }

        [HttpDelete("admin-appointments/delete/{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteAdminAppointment(Guid id)
        {
            var ownerId = User.GetUserId();
            var owner = await _context.BarbershopOwners.FindAsync(ownerId);
            if (owner == null) return NotFound(new { message = "Owner not found" });

            if (!owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Удаление записей заблокировано." });

            var appointment = await _context.Appointments.Include(a => a.Master).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null || appointment.Master.OwnerId != ownerId)
                return NotFound(new { message = "Appointment not found" });

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Appointment deleted successfully" });
        }

        // ─── Owner: Admin Services ─────────────────────────────────────────────────

        [HttpGet("admin-services/{masterId}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetAdminServices(Guid masterId)
        {
            var ownerId = User.GetUserId();

            var master = await _context.Masters.AsNoTracking().FirstOrDefaultAsync(m => m.Id == masterId && m.OwnerId == ownerId);
            if (master == null) return NotFound(new { message = "Master not found" });

            var serviceNames = await _context.ServiceNames.AsNoTracking().Where(sn => sn.OwnerId == ownerId).ToListAsync();
            var myServices = await _context.Services.AsNoTracking().Where(s => s.MasterId == masterId).ToListAsync();

            var response = serviceNames.Select(sn =>
            {
                var service = myServices.FirstOrDefault(s => s.ServiceNameId == sn.Id);
                return new MasterServiceDto
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

        [HttpPost("put-photo/{appointmentId}")]
        [Authorize(Roles = "Master,Owner")]
        public async Task<IActionResult> PostPhoto(Guid appointmentId, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(new { message = "Photo is required" });

            if (photo.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "Максимальный размер фото: 10 МБ" });

            var extension = Path.GetExtension(photo.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
                return BadRequest(new { message = "Недопустимый формат файла. Разрешены только JPG, PNG, WEBP." });

            var userId = User.GetUserId();
            var isOwner = User.IsInRole("Owner");

            var appointment = await _context.Appointments
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            if (isOwner && appointment.Master.OwnerId != userId)
                return Unauthorized();

            if (!isOwner && appointment.MasterId != userId)
                return Unauthorized();

            if (appointment.Master?.Owner == null || !appointment.Master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна. Действие заблокировано." });

            string dir = Path.Combine("wwwroot", "results");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fileName = Guid.NewGuid().ToString();
            string path = Path.Combine(dir, $"{fileName}{extension}");

            using var readstream = photo.OpenReadStream();
            using var fs = new FileStream(path, FileMode.Create);
            await readstream.CopyToAsync(fs);

            string photoUrl = $"/results/{fileName}{extension}";
            appointment.PhotoResultUrl = photoUrl;
            await _context.SaveChangesAsync();

            return Ok(new { photoUrl });
        }

        [HttpGet("get-appointments")]
        [Authorize(Roles = "Master,Owner")]
        public async Task<IActionResult> GetAllAppointments([FromQuery] string tgId)
        {
            var userId = User.GetUserId();
            var isOwner = User.IsInRole("Owner");

            Guid ownerId;
            if (isOwner)
            {
                ownerId = userId;
                var owner = await _context.BarbershopOwners.FindAsync(ownerId);
                if (owner == null || !owner.HasActiveSubscription())
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });
            }
            else
            {
                var master = await _context.Masters.Include(m => m.Owner).AsNoTracking().FirstOrDefaultAsync(m => m.Id == userId);
                if (master == null) return NotFound(new { message = "Master not found" });
                if (master.Owner == null || !master.Owner.HasActiveSubscription())
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });
                ownerId = master.OwnerId;
            }

            var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.TelegramId == tgId && c.OwnerId == ownerId);
            if (client == null) return NotFound(new { message = "Client not found" });

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                    .ThenInclude(s => s.ServiceName)
                .Where(a => a.ClientId == client.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    ClientId = a.ClientId,
                    ClientName = client.Name,
                    ClientTelegramId = client.TelegramId,
                    MasterId = a.MasterId,
                    AppointmentDate = a.AppointmentDate,
                    AppointmentEndDate = a.AppointmentEndDate,
                    Status = a.Status,
                    ReminderSent = a.ReminderSent,
                    ServiceId = a.ServiceId,
                    ServiceName = a.Service != null ? a.Service.ServiceName.Name : null,
                    PhotoResultUrl = a.PhotoResultUrl,
                    ResultNote = a.ResultNote
                })
                .ToListAsync();

            return Ok(new
            {
                client = new { client.Id, client.Name, client.TelegramId, client.Phone, client.Notes },
                appointments
            });
        }

        // ─── Master/Owner: Appointment Comments ──────────────────────────────────

        [HttpPost("appointment-comment/{appointmentId}")]
        [Authorize(Roles = "Master,Owner")]
        public async Task<IActionResult> AddAppointmentComment(Guid appointmentId, [FromBody] AppointmentCommentRequest request)
        {
            var userId = User.GetUserId();
            var isOwner = User.IsInRole("Owner");

            var appointment = await _context.Appointments
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            if (isOwner && appointment.Master.OwnerId != userId)
                return Unauthorized();
            if (!isOwner && appointment.MasterId != userId)
                return Unauthorized();

            if (appointment.Master?.Owner == null || !appointment.Master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            appointment.ResultNote = request.Comment;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comment added successfully", comment = appointment.ResultNote });
        }

        [HttpGet("appointment-comment/{appointmentId}")]
        [Authorize(Roles = "Master,Owner")]
        public async Task<IActionResult> GetAppointmentComment(Guid appointmentId)
        {
            var userId = User.GetUserId();
            var isOwner = User.IsInRole("Owner");

            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            if (isOwner && appointment.Master.OwnerId != userId)
                return Unauthorized();
            if (!isOwner && appointment.MasterId != userId)
                return Unauthorized();

            if (appointment.Master?.Owner == null || !appointment.Master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            return Ok(new { appointmentId = appointment.Id, comment = appointment.ResultNote });
        }

        [HttpPut("appointment-comment/{appointmentId}")]
        [Authorize(Roles = "Master,Owner")]
        public async Task<IActionResult> UpdateAppointmentComment(Guid appointmentId, [FromBody] AppointmentCommentRequest request)
        {
            var userId = User.GetUserId();
            var isOwner = User.IsInRole("Owner");

            var appointment = await _context.Appointments
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            if (isOwner && appointment.Master.OwnerId != userId)
                return Unauthorized();
            if (!isOwner && appointment.MasterId != userId)
                return Unauthorized();

            if (appointment.Master?.Owner == null || !appointment.Master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            appointment.ResultNote = request.Comment;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comment updated successfully", comment = appointment.ResultNote });
        }

        [HttpDelete("appointment-comment/{appointmentId}")]
        [Authorize(Roles = "Master,Owner")]
        public async Task<IActionResult> DeleteAppointmentComment(Guid appointmentId)
        {
            var userId = User.GetUserId();
            var isOwner = User.IsInRole("Owner");

            var appointment = await _context.Appointments
                .Include(a => a.Master)
                    .ThenInclude(m => m.Owner)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null)
                return NotFound(new { message = "Appointment not found" });

            if (isOwner && appointment.Master.OwnerId != userId)
                return Unauthorized();
            if (!isOwner && appointment.MasterId != userId)
                return Unauthorized();

            if (appointment.Master?.Owner == null || !appointment.Master.Owner.HasActiveSubscription())
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Подписка заведения не активна." });

            appointment.ResultNote = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comment deleted successfully" });
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
            public string? Comment { get; set; }
        }

        public class AppointmentCommentRequest
        {
            public string? Comment { get; set; }
        }
    }
}