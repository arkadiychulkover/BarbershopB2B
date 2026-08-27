using Backend.Data;
using Backend.DTOs;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Enums;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ExcelExportService _excelExportService;
        private readonly StatisticService _statisticService;

        public AdminController(
            AppDbContext context, 
            ExcelExportService excelExportService,
            StatisticService statisticService)
        {
            _context = context;
            _excelExportService = excelExportService;
            _statisticService = statisticService;
        }

        [HttpGet("check")]
        public IActionResult CheckAdminRole()
        {
            var email = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                     ?? User.FindFirstValue(ClaimTypes.Email)
                     ?? User.FindFirstValue("sub")
                     ?? "admin";

            return Ok(new
            {
                role = "Admin",
                message = "Доступ администратора подтвержден",
                email = email
            });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetPlatformStats()
        {
            var totalOwners = await _context.BarbershopOwners.CountAsync();
            var activeOwners = await _context.BarbershopOwners.CountAsync(o => o.Status == OwnerStatus.Active && !o.IsBlocked && o.NextPayment > DateTime.UtcNow);
            var inactiveOwners = totalOwners - activeOwners;

            var totalMasters = await _context.Masters.CountAsync();
            var activeMasters = await _context.Masters.CountAsync(m => m.IsActive);

            var totalClients = await _context.Clients.CountAsync(c => c.TelegramId != "WALKIN");

            var totalAppointments = await _context.Appointments.CountAsync();
            var completedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed);

            var totalRevenue = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .SumAsync(a => (decimal?)a.Service.Price) ?? 0m;

            return Ok(new AdminStatsDto
            {
                TotalOwners = totalOwners,
                ActiveOwners = activeOwners,
                InactiveOwners = inactiveOwners,
                TotalMasters = totalMasters,
                ActiveMasters = activeMasters,
                TotalClients = totalClients,
                TotalAppointments = totalAppointments,
                CompletedAppointments = completedAppointments,
                TotalRevenue = totalRevenue
            });
        }

        [HttpGet("owners")]
        public async Task<IActionResult> GetAllOwners([FromQuery] string? search, [FromQuery] string? status)
        {
            var query = _context.BarbershopOwners
                .AsNoTracking()
                .Include(o => o.Masters)
                .Include(o => o.Clients)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.OwnerName.ToLower().Contains(s) 
                                      || o.Email.ToLower().Contains(s)
                                      || o.BarbershopName.ToLower().Contains(s)
                                      || (o.BotUsername != null && o.BotUsername.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OwnerStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(o => o.Status == parsedStatus);
            }

            var owners = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var ownerIds = owners.Select(o => o.Id).ToList();

            var appointmentsByOwner = await _context.Appointments
                .Where(a => a.Master != null && ownerIds.Contains(a.Master.OwnerId))
                .GroupBy(a => a.Master.OwnerId)
                .Select(g => new
                {
                    OwnerId = g.Key,
                    TotalAppointments = g.Count(),
                    Turnover = g.Where(a => a.Status == AppointmentStatus.Completed).Sum(a => (decimal?)a.Service.Price) ?? 0m
                })
                .ToDictionaryAsync(x => x.OwnerId, x => x);

            var dtos = owners.Select(o =>
            {
                appointmentsByOwner.TryGetValue(o.Id, out var apptStats);

                return new AdminOwnerDto
                {
                    Id = o.Id,
                    OwnerName = o.OwnerName,
                    Email = o.Email,
                    PhoneNumber = o.PhoneNumber,
                    BarbershopName = o.BarbershopName,
                    BarbershopAddress = o.BarbershopAddress,
                    BarbershopDescription = o.BarbershopDescription,
                    BotToken = o.BotToken,
                    BotUsername = o.BotUsername,
                    Status = o.Status.ToString(),
                    IsBlocked = o.IsBlocked,
                    NextPayment = o.NextPayment,
                    CreatedAt = o.CreatedAt,
                    MastersCount = o.Masters?.Count ?? 0,
                    ClientsCount = o.Clients?.Count(c => c.TelegramId != "WALKIN") ?? 0,
                    AppointmentsCount = apptStats?.TotalAppointments ?? 0,
                    Turnover = apptStats?.Turnover ?? 0m
                };
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("owners/{id}")]
        public async Task<IActionResult> GetOwnerDetails(Guid id)
        {
            var owner = await _context.BarbershopOwners
                .AsNoTracking()
                .Include(o => o.Masters)
                .Include(o => o.Clients)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (owner == null)
                return NotFound(new { message = "Владелец заведения не найден" });

            var totalAppointments = await _context.Appointments
                .Where(a => a.Master != null && a.Master.OwnerId == id)
                .CountAsync();

            var turnover = await _context.Appointments
                .Where(a => a.Master != null && a.Master.OwnerId == id && a.Status == AppointmentStatus.Completed)
                .SumAsync(a => (decimal?)a.Service.Price) ?? 0m;

            var dto = new AdminOwnerDto
            {
                Id = owner.Id,
                OwnerName = owner.OwnerName,
                Email = owner.Email,
                PhoneNumber = owner.PhoneNumber,
                BarbershopName = owner.BarbershopName,
                BarbershopAddress = owner.BarbershopAddress,
                BarbershopDescription = owner.BarbershopDescription,
                BotToken = owner.BotToken,
                BotUsername = owner.BotUsername,
                Status = owner.Status.ToString(),
                IsBlocked = owner.IsBlocked,
                NextPayment = owner.NextPayment,
                CreatedAt = owner.CreatedAt,
                MastersCount = owner.Masters?.Count ?? 0,
                ClientsCount = owner.Clients?.Count(c => c.TelegramId != "WALKIN") ?? 0,
                AppointmentsCount = totalAppointments,
                Turnover = turnover
            };

            return Ok(dto);
        }

        [HttpPost("owners")]
        public async Task<IActionResult> CreateOwner([FromBody] CreateOwnerByAdminRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email и пароль обязательны" });

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(cleanEmail))
                return BadRequest(new { message = "Некорректный формат email" });

            if (request.Password.Length < 6)
                return BadRequest(new { message = "Пароль должен содержать минимум 6 символов" });

            bool emailExists = await _context.BarbershopOwners.AnyAsync(o => o.Email.ToLower() == cleanEmail);
            if (emailExists)
                return BadRequest(new { message = "Заведение с таким email уже существует" });

            bool adminExists = await _context.SaasAdmins.AnyAsync(a => a.Email.ToLower() == cleanEmail);
            if (adminExists)
                return BadRequest(new { message = "Пользователь с таким email уже зарегистрирован как администратор" });

            var days = request.SubscriptionDays > 0 ? request.SubscriptionDays : 30;
            var now = DateTime.UtcNow;
            var defaultName = cleanEmail.Split('@')[0];

            var owner = new BarbershopOwner
            {
                Id = Guid.NewGuid(),
                Email = cleanEmail,
                PasswordHash = PasswordSecurity.HashPassword(request.Password),
                OwnerName = defaultName,
                BarbershopName = $"Барбершоп ({defaultName})",
                BarbershopAddress = "",
                BarbershopDescription = "",
                PhoneNumber = "",
                TelegramId = "",
                BotToken = "",
                BotUsername = "",
                WalletAddress = "",
                TimeZone = "Europe/Kyiv",
                ReminderHoursBefore = 2,
                DepositEnabled = false,
                Status = OwnerStatus.Active,
                IsBlocked = false,
                CreatedAt = now,
                PayedAt = now,
                LastPayment = now,
                NextPayment = now.AddDays(days),
                MasterFee = 0m
            };

            _context.BarbershopOwners.Add(owner);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Заведение успешно создано",
                owner = new AdminOwnerDto
                {
                    Id = owner.Id,
                    OwnerName = owner.OwnerName,
                    Email = owner.Email,
                    PhoneNumber = owner.PhoneNumber,
                    BarbershopName = owner.BarbershopName,
                    BarbershopAddress = owner.BarbershopAddress,
                    BarbershopDescription = owner.BarbershopDescription,
                    BotToken = owner.BotToken,
                    BotUsername = owner.BotUsername,
                    Status = owner.Status.ToString(),
                    IsBlocked = owner.IsBlocked,
                    NextPayment = owner.NextPayment,
                    CreatedAt = owner.CreatedAt,
                    MastersCount = 0,
                    ClientsCount = 0,
                    AppointmentsCount = 0,
                    Turnover = 0m
                }
            });
        }

        [HttpPut("owners/{id}/subscription")]
        public async Task<IActionResult> UpdateOwnerSubscription(Guid id, [FromBody] UpdateOwnerSubscriptionRequest request)
        {
            var owner = await _context.BarbershopOwners.FindAsync(id);
            if (owner == null)
                return NotFound(new { message = "Владелец заведения не найден" });

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                {
                    owner.Status = OwnerStatus.Active;
                    owner.IsBlocked = false;
                    if (owner.NextPayment <= DateTime.UtcNow)
                    {
                        owner.NextPayment = DateTime.UtcNow.AddMonths(1);
                    }
                }
                else
                {
                    owner.Status = OwnerStatus.Frozen;
                    owner.NextPayment = DateTime.UtcNow.AddDays(-1);
                }
            }

            if (request.IsBlocked.HasValue)
            {
                owner.IsBlocked = request.IsBlocked.Value;
            }

            if (request.NextPayment.HasValue)
            {
                owner.NextPayment = request.NextPayment.Value;
                owner.Status = owner.NextPayment > DateTime.UtcNow ? OwnerStatus.Active : OwnerStatus.Frozen;
            }

            if (request.ExtendDays.HasValue && request.ExtendDays.Value > 0)
            {
                var baseDate = owner.NextPayment > DateTime.UtcNow
                    ? owner.NextPayment
                    : DateTime.UtcNow;

                owner.NextPayment = baseDate.AddDays(request.ExtendDays.Value);
                owner.Status = OwnerStatus.Active;
                owner.IsBlocked = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Подписка успешно обновлена",
                status = owner.Status.ToString(),
                isBlocked = owner.IsBlocked,
                nextPayment = owner.NextPayment
            });
        }

        [HttpPut("owners/{id}")]
        public async Task<IActionResult> UpdateOwnerProfile(Guid id, [FromBody] UpdateOwnerProfileRequest request)
        {
            var owner = await _context.BarbershopOwners.FindAsync(id);
            if (owner == null)
                return NotFound(new { message = "Владелец заведения не найден" });

            if (!string.IsNullOrWhiteSpace(request.OwnerName)) owner.OwnerName = request.OwnerName.Trim();
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var cleanedPhone = Regex.Replace(request.PhoneNumber.Trim(), @"[\s\-\(\)]", "");
                var phoneRegex = new Regex(@"^\+[0-9]{1,3}[0-9]{9}$");
                if (!phoneRegex.IsMatch(cleanedPhone))
                {
                    return BadRequest(new { message = "Некорректный номер телефона. Номер должен начинаться с \"+\", содержать код страны (1-3 цифры) и 9 цифр номера (например, +380991234567 или +79991234567)." });
                }
                owner.PhoneNumber = cleanedPhone;
            }
            if (!string.IsNullOrWhiteSpace(request.BarbershopName)) owner.BarbershopName = request.BarbershopName.Trim();
            if (request.BarbershopAddress != null) owner.BarbershopAddress = request.BarbershopAddress.Trim();
            if (request.BarbershopDescription != null) owner.BarbershopDescription = request.BarbershopDescription.Trim();
            if (request.BotToken != null) owner.BotToken = request.BotToken.Trim();
            if (request.BotUsername != null) owner.BotUsername = request.BotUsername.Trim().TrimStart('@');
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (request.NewPassword.Trim().Length < 6)
                {
                    return BadRequest(new { message = "Новый пароль должен содержать не менее 6 символов." });
                }
                owner.PasswordHash = PasswordSecurity.HashPassword(request.NewPassword.Trim());
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Данные заведения успешно обновлены" });
        }

        [HttpDelete("owners/{id}")]
        public async Task<IActionResult> DeleteOwner(Guid id)
        {
            var owner = await _context.BarbershopOwners.FindAsync(id);
            if (owner == null)
                return NotFound(new { message = "Владелец заведения не найден" });

            _context.BarbershopOwners.Remove(owner);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Заведение и все связанные данные успешно удалены" });
        }

        [HttpGet("masters")]
        public async Task<IActionResult> GetAllMasters([FromQuery] Guid? ownerId, [FromQuery] string? search)
        {
            var query = _context.Masters
                .AsNoTracking()
                .Include(m => m.Owner)
                .Include(m => m.Appointments)
                .AsQueryable();

            if (ownerId.HasValue && ownerId.Value != Guid.Empty)
            {
                query = query.Where(m => m.OwnerId == ownerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(m => m.Name.ToLower().Contains(s) 
                                      || (m.TelegramUsername != null && m.TelegramUsername.ToLower().Contains(s))
                                      || (m.TelegramId != null && m.TelegramId.Contains(s))
                                      || (m.Owner != null && m.Owner.BarbershopName.ToLower().Contains(s)));
            }

            var masters = await query.OrderByDescending(m => m.Id).ToListAsync();

            var dtos = masters.Select(m => new AdminMasterDto
            {
                Id = m.Id,
                OwnerId = m.OwnerId,
                OwnerName = m.Owner?.OwnerName ?? "Не указан",
                BarbershopName = m.Owner?.BarbershopName ?? "Не указан",
                Name = m.Name,
                Description = m.Description,
                TelegramId = m.TelegramId,
                TelegramUsername = m.TelegramUsername,
                PhotoUrl = m.PhotoUrl,
                IsActive = m.IsActive,
                Rating = m.Rating,
                ReviewsCount = m.ReviewsCount,
                AppointmentsCount = m.Appointments?.Count ?? 0
            }).ToList();

            return Ok(dtos);
        }

        [HttpPut("masters/{id}")]
        public async Task<IActionResult> UpdateMaster(Guid id, [FromBody] UpdateAdminMasterRequest request)
        {
            var master = await _context.Masters.FindAsync(id);
            if (master == null)
                return NotFound(new { message = "Мастер не найден" });

            if (!string.IsNullOrWhiteSpace(request.Name)) master.Name = request.Name.Trim();
            if (request.Description != null) master.Description = request.Description.Trim();
            if (request.TelegramId != null) master.TelegramId = request.TelegramId.Trim();
            if (request.TelegramUsername != null) master.TelegramUsername = request.TelegramUsername.Trim().TrimStart('@');
            if (request.IsActive.HasValue) master.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Мастер успешно обновлен" });
        }

        [HttpDelete("masters/{id}")]
        public async Task<IActionResult> DeleteMaster(Guid id)
        {
            var master = await _context.Masters.FindAsync(id);
            if (master == null)
                return NotFound(new { message = "Мастер не найден" });

            _context.Masters.Remove(master);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Мастер успешно удален" });
        }

        [HttpGet("clients")]
        public async Task<IActionResult> GetAllClients([FromQuery] Guid? ownerId, [FromQuery] string? search)
        {
            var query = _context.Clients
                .AsNoTracking()
                .Include(c => c.Owner)
                .Include(c => c.Appointments)
                .Where(c => c.TelegramId != "WALKIN")
                .AsQueryable();

            if (ownerId.HasValue && ownerId.Value != Guid.Empty)
            {
                query = query.Where(c => c.OwnerId == ownerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(s)
                                      || (c.Phone != null && c.Phone.Contains(s))
                                      || c.TelegramId.Contains(s)
                                      || (c.Owner != null && c.Owner.BarbershopName.ToLower().Contains(s)));
            }

            var clients = await query.OrderByDescending(c => c.Id).Take(200).ToListAsync();

            var dtos = clients.Select(c => new AdminClientDto
            {
                Id = c.Id,
                OwnerId = c.OwnerId,
                BarbershopName = c.Owner?.BarbershopName ?? "Не указан",
                Name = c.Name,
                Phone = c.Phone,
                TelegramId = c.TelegramId,
                AppointmentsCount = c.Appointments?.Count ?? 0
            }).ToList();

            return Ok(dtos);
        }

        [HttpDelete("clients/{id}")]
        public async Task<IActionResult> DeleteClient(Guid id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound(new { message = "Клиент не найден" });

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Клиент успешно удален" });
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAllAppointments([FromQuery] Guid? ownerId, [FromQuery] AppointmentStatus? status, [FromQuery] int limit = 100)
        {
            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Master).ThenInclude(m => m.Owner)
                .Include(a => a.Client)
                .Include(a => a.Service).ThenInclude(s => s.ServiceName)
                .AsQueryable();

            if (ownerId.HasValue && ownerId.Value != Guid.Empty)
            {
                query = query.Where(a => a.Master.OwnerId == ownerId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .Take(Math.Min(limit, 300))
                .ToListAsync();

            var dtos = appointments.Select(a => new AdminAppointmentDto
            {
                Id = a.Id,
                OwnerId = a.Master?.OwnerId ?? Guid.Empty,
                BarbershopName = a.Master?.Owner?.BarbershopName ?? "—",
                MasterName = a.Master?.Name ?? "—",
                ClientName = a.Client?.Name ?? "Гость",
                ClientPhone = a.Client?.Phone,
                ServiceName = a.Service?.ServiceName?.Name ?? "Услуга",
                Price = a.Service?.Price ?? 0m,
                AppointmentDate = a.AppointmentDate,
                AppointmentEndDate = a.AppointmentEndDate,
                Status = a.Status
            }).ToList();

            return Ok(dtos);
        }

        [HttpDelete("appointments/{id}")]
        public async Task<IActionResult> DeleteAppointment(Guid id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { message = "Запись не найдена" });

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Запись успешно удалена" });
        }

        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins()
        {
            var admins = await _context.SaasAdmins
                .AsNoTracking()
                .OrderBy(a => a.CreatedAt)
                .Select(a => new AdminDto
                {
                    Id = a.Id,
                    Email = a.Email,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(admins);
        }

        [HttpPost("admins")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email и пароль обязательны" });

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            bool exists = await _context.SaasAdmins.AnyAsync(a => a.Email.ToLower() == cleanEmail);
            if (exists)
                return BadRequest(new { message = "Администратор с таким email уже существует" });

            var (salt, hash) = PasswordSecurity.CreateHashAndSalt(request.Password);

            var newAdmin = new SaasAdmin
            {
                Id = Guid.NewGuid(),
                Email = cleanEmail,
                PasswordSalt = salt,
                PasswordHash = hash,
                CreatedAt = DateTime.UtcNow
            };

            _context.SaasAdmins.Add(newAdmin);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Администратор успешно создан",
                admin = new AdminDto
                {
                    Id = newAdmin.Id,
                    Email = newAdmin.Email,
                    CreatedAt = newAdmin.CreatedAt
                }
            });
        }

        [HttpGet("chart-analytics")]
        public async Task<IActionResult> GetChartAnalytics(
            [FromQuery] Guid? ownerId, 
            [FromQuery] string? period = "30d", 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            var analytics = await _statisticService.GetChartAnalyticsAsync(ownerId, period ?? "30d", startDate, endDate);
            return Ok(analytics);
        }

        [HttpGet("export/clients")]
        public async Task<IActionResult> ExportClients([FromQuery] Guid? ownerId)
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
