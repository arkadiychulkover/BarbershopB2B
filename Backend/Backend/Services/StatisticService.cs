using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class DailyStatisticDto
    {
        public decimal TotalProfit { get; set; }
        public decimal OwnerProfit { get; set; }
        public int ClientsCount { get; set; }
    }

    public class StatisticService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public StatisticService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Dictionary<DateTime, DailyStatisticDto>> GetOwnerStatisticByDate(Guid ownerId, DateTime startDate, DateTime endDate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            var appointments = await context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                .Where(a => a.Master.OwnerId == ownerId
                         && a.AppointmentDate >= start
                         && a.AppointmentDate < end
                         && a.Status != Models.Enums.AppointmentStatus.Cancelled)
                .Select(a => new
                {
                    a.AppointmentDate,
                    Price = a.Service != null ? a.Service.Price : (a.OwnerProfit + a.MasterProfit),
                    OwnerProfit = a.OwnerProfit > 0 ? a.OwnerProfit : (a.Service != null ? a.Service.Price : 0),
                    a.ClientId
                })
                .ToListAsync();

            var grouped = appointments
                .GroupBy(a => a.AppointmentDate.Date)
                .ToDictionary(
                    g => g.Key,
                    g => new DailyStatisticDto
                    {
                        TotalProfit = g.Sum(a => a.Price),
                        OwnerProfit = g.Sum(a => a.OwnerProfit),
                        ClientsCount = g.Select(a => a.ClientId).Distinct().Count()
                    }
                );

            var result = new Dictionary<DateTime, DailyStatisticDto>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (grouped.TryGetValue(date, out var stat))
                {
                    result[date] = stat;
                }
                else
                {
                    result[date] = new DailyStatisticDto { TotalProfit = 0, OwnerProfit = 0, ClientsCount = 0 };
                }
            }

            return result;
        }

        public async Task<Dictionary<int, DailyStatisticDto>> GetOwnerStatisticByHour(Guid ownerId, DateTime date)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var targetDate = date.Date;

            var appointments = await context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                .Where(a => a.Master.OwnerId == ownerId
                         && a.AppointmentDate >= targetDate
                         && a.AppointmentDate < targetDate.AddDays(1)
                         && a.Status != Models.Enums.AppointmentStatus.Cancelled)
                .Select(a => new
                {
                    a.AppointmentDate,
                    Price = a.Service != null ? a.Service.Price : (a.OwnerProfit + a.MasterProfit),
                    OwnerProfit = a.OwnerProfit > 0 ? a.OwnerProfit : (a.Service != null ? a.Service.Price : 0),
                    a.ClientId
                })
                .ToListAsync();

            var grouped = appointments
                .GroupBy(a => a.AppointmentDate.Hour)
                .ToDictionary(
                    g => g.Key,
                    g => new DailyStatisticDto
                    {
                        TotalProfit = g.Sum(a => a.Price),
                        OwnerProfit = g.Sum(a => a.OwnerProfit),
                        ClientsCount = g.Select(a => a.ClientId).Distinct().Count()
                    }
                );

            var result = new Dictionary<int, DailyStatisticDto>();
            for (int i = 0; i < 24; i++)
            {
                if (grouped.TryGetValue(i, out var stat))
                    result[i] = stat;
                else
                    result[i] = new DailyStatisticDto { TotalProfit = 0, OwnerProfit = 0, ClientsCount = 0 };
            }

            return result;
        }

        public async Task<Dictionary<DateTime, DailyStatisticDto>> GetBarberStatisticByDate(Guid masterId, Guid ownerId, DateTime startDate, DateTime endDate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            var appointments = await context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                .Where(a => a.MasterId == masterId
                         && a.Master.OwnerId == ownerId
                         && a.AppointmentDate >= start
                         && a.AppointmentDate < end
                         && a.Status != Models.Enums.AppointmentStatus.Cancelled)
                .Select(a => new
                {
                    a.AppointmentDate,
                    Price = a.Service != null ? a.Service.Price : (a.OwnerProfit + a.MasterProfit),
                    OwnerProfit = a.OwnerProfit > 0 ? a.OwnerProfit : (a.Service != null ? a.Service.Price : 0),
                    a.ClientId
                })
                .ToListAsync();

            var grouped = appointments
                .GroupBy(a => a.AppointmentDate.Date)
                .ToDictionary(
                    g => g.Key,
                    g => new DailyStatisticDto
                    {
                        TotalProfit = g.Sum(a => a.Price),
                        OwnerProfit = g.Sum(a => a.OwnerProfit),
                        ClientsCount = g.Select(a => a.ClientId).Distinct().Count()
                    }
                );

            var result = new Dictionary<DateTime, DailyStatisticDto>();
            for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
            {
                if (grouped.TryGetValue(d, out var stat))
                    result[d] = stat;
                else
                    result[d] = new DailyStatisticDto { TotalProfit = 0, OwnerProfit = 0, ClientsCount = 0 };
            }

            return result;
        }

        public async Task<Dictionary<int, DailyStatisticDto>> GetBarberStatisticByHour(Guid masterId, Guid ownerId, DateTime date)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var targetDate = date.Date;

            var appointments = await context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                .Where(a => a.MasterId == masterId
                         && a.Master.OwnerId == ownerId
                         && a.AppointmentDate >= targetDate
                         && a.AppointmentDate < targetDate.AddDays(1)
                         && a.Status != Models.Enums.AppointmentStatus.Cancelled)
                .Select(a => new
                {
                    a.AppointmentDate,
                    Price = a.Service != null ? a.Service.Price : (a.OwnerProfit + a.MasterProfit),
                    OwnerProfit = a.OwnerProfit > 0 ? a.OwnerProfit : (a.Service != null ? a.Service.Price : 0),
                    a.ClientId
                })
                .ToListAsync();

            var grouped = appointments
                .GroupBy(a => a.AppointmentDate.Hour)
                .ToDictionary(
                    g => g.Key,
                    g => new DailyStatisticDto
                    {
                        TotalProfit = g.Sum(a => a.Price),
                        OwnerProfit = g.Sum(a => a.OwnerProfit),
                        ClientsCount = g.Select(a => a.ClientId).Distinct().Count()
                    }
                );

            var result = new Dictionary<int, DailyStatisticDto>();
            for (int i = 0; i < 24; i++)
            {
                if (grouped.TryGetValue(i, out var stat))
                    result[i] = stat;
                else
                    result[i] = new DailyStatisticDto { TotalProfit = 0, OwnerProfit = 0, ClientsCount = 0 };
            }

            return result;
        }
    }
}
