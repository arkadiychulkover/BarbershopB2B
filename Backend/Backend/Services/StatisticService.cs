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

    public class ChartDataPointDto
    {
        public string Label { get; set; } = string.Empty;
        public string FullDate { get; set; } = string.Empty;
        public int Visits { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ChartAnalyticsDto
    {
        public List<ChartDataPointDto> Points { get; set; } = new();
        public int TotalVisits { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageDailyRevenue { get; set; }
        public decimal AverageDailyVisits { get; set; }
        public string PeakDate { get; set; } = string.Empty;
        public decimal PeakRevenue { get; set; }
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

        public async Task<ChartAnalyticsDto> GetChartAnalyticsAsync(
            Guid? ownerId, 
            string? period, 
            DateTime? customStartDate = null, 
            DateTime? customEndDate = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            DateTime startDate;
            DateTime endDate = now.Date.AddDays(1);
            bool isYearOrMonthly = false;

            if (period?.ToLower() == "custom" && customStartDate.HasValue && customEndDate.HasValue)
            {
                startDate = DateTime.SpecifyKind(customStartDate.Value.Date, DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(customEndDate.Value.Date.AddDays(1), DateTimeKind.Utc);

                if (startDate > endDate)
                {
                    var temp = startDate;
                    startDate = endDate.AddDays(-1);
                    endDate = temp.AddDays(1);
                }

                var totalDays = (int)(endDate.Date - startDate.Date).TotalDays;
                if (totalDays > 120)
                {
                    isYearOrMonthly = true;
                }
            }
            else
            {
                switch (period?.ToLower())
                {
                    case "7d":
                        startDate = now.Date.AddDays(-6);
                        break;
                    case "90d":
                        startDate = now.Date.AddDays(-89);
                        break;
                    case "year":
                        startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
                        isYearOrMonthly = true;
                        break;
                    case "30d":
                    default:
                        startDate = now.Date.AddDays(-29);
                        break;
                }
            }

            var query = context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate < endDate && a.Status != Models.Enums.AppointmentStatus.Cancelled);

            if (ownerId.HasValue && ownerId.Value != Guid.Empty)
            {
                query = query.Where(a => a.Master.OwnerId == ownerId.Value);
            }

            var appointments = await query
                .Select(a => new
                {
                    a.AppointmentDate,
                    Price = a.Service != null ? a.Service.Price : (a.OwnerProfit + a.MasterProfit),
                    a.Status
                })
                .ToListAsync();

            var points = new List<ChartDataPointDto>();

            if (isYearOrMonthly)
            {
                var currentMonth = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                while (currentMonth < endDate)
                {
                    var nextMonth = currentMonth.AddMonths(1);
                    var monthAppts = appointments.Where(a => a.AppointmentDate >= currentMonth && a.AppointmentDate < nextMonth).ToList();
                    var visits = monthAppts.Count;
                    var revenue = monthAppts.Where(a => a.Status == Models.Enums.AppointmentStatus.Completed).Sum(a => a.Price);

                    points.Add(new ChartDataPointDto
                    {
                        Label = currentMonth.ToString("MMM yyyy"),
                        FullDate = currentMonth.ToString("yyyy-MM"),
                        Visits = visits,
                        Revenue = revenue
                    });

                    currentMonth = nextMonth;
                }
            }
            else
            {
                var totalDays = Math.Max(1, (int)(endDate.Date - startDate.Date).TotalDays);
                for (int i = 0; i < totalDays; i++)
                {
                    var day = startDate.Date.AddDays(i);
                    var dayEnd = day.AddDays(1);

                    var dayAppts = appointments.Where(a => a.AppointmentDate >= day && a.AppointmentDate < dayEnd).ToList();
                    var visits = dayAppts.Count;
                    var revenue = dayAppts.Where(a => a.Status == Models.Enums.AppointmentStatus.Completed).Sum(a => a.Price);

                    points.Add(new ChartDataPointDto
                    {
                        Label = day.ToString("dd.MM"),
                        FullDate = day.ToString("yyyy-MM-dd"),
                        Visits = visits,
                        Revenue = revenue
                    });
                }
            }

            var totalVisits = points.Sum(p => p.Visits);
            var totalRevenue = points.Sum(p => p.Revenue);
            var daysCount = Math.Max(points.Count, 1);
            var peakPoint = points.OrderByDescending(p => p.Revenue).FirstOrDefault();

            return new ChartAnalyticsDto
            {
                Points = points,
                TotalVisits = totalVisits,
                TotalRevenue = totalRevenue,
                AverageDailyVisits = Math.Round((decimal)totalVisits / daysCount, 1),
                AverageDailyRevenue = Math.Round(totalRevenue / daysCount, 2),
                PeakDate = peakPoint?.Label ?? "—",
                PeakRevenue = peakPoint?.Revenue ?? 0m
            };
        }
    }
}
