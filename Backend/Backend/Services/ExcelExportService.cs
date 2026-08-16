using ClosedXML.Excel;
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Backend.Models.Enums;

namespace Backend.Services
{
    public class ClientExportDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string TelegramId { get; set; } = string.Empty;
        public string? BarbershopName { get; set; }
        public string? OwnerName { get; set; }
        public int TotalVisits { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? FirstVisit { get; set; }
        public DateTime? LastVisit { get; set; }
    }

    public class ExcelExportService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ExcelExportService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<byte[]> ExportOwnerClientsToExcelAsync(Guid ownerId, string barbershopName)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var clients = await context.Clients
                .AsNoTracking()
                .Where(c => c.OwnerId == ownerId && c.TelegramId != "WALKIN")
                .Include(c => c.Appointments).ThenInclude(a => a.Service)
                .OrderByDescending(c => c.Appointments.Count)
                .ToListAsync();

            var exportList = clients.Select(c =>
            {
                var validAppointments = c.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .OrderBy(a => a.AppointmentDate)
                    .ToList();

                var totalSpent = validAppointments.Sum(a => a.Service?.Price ?? (a.OwnerProfit + a.MasterProfit));
                var firstVisit = validAppointments.FirstOrDefault()?.AppointmentDate;
                var lastVisit = validAppointments.LastOrDefault()?.AppointmentDate;

                return new ClientExportDto
                {
                    Name = c.Name,
                    Phone = c.Phone ?? "Не указан",
                    TelegramId = c.TelegramId,
                    TotalVisits = c.Appointments.Count(a => a.Status != AppointmentStatus.Cancelled),
                    TotalSpent = totalSpent,
                    FirstVisit = firstVisit,
                    LastVisit = lastVisit
                };
            }).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Клиенты");

            worksheet.Cell(1, 1).Value = $"База клиентов — {barbershopName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

            worksheet.Cell(2, 1).Value = $"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm} (Всего клиентов: {exportList.Count})";
            worksheet.Cell(2, 1).Style.Font.FontSize = 10;
            worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");

            int headerRow = 4;
            string[] headers = new[]
            {
                "№",
                "Имя клиента",
                "Телефон",
                "Telegram ID",
                "Всего визитов",
                "Сумма покупок (₴)",
                "Первый визит",
                "Последний визит"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DF9E8E");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            worksheet.Row(headerRow).Height = 24;

            int currentRow = headerRow + 1;
            for (int i = 0; i < exportList.Count; i++)
            {
                var item = exportList[i];
                worksheet.Cell(currentRow, 1).Value = i + 1;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 2).Value = item.Name;
                worksheet.Cell(currentRow, 3).Value = item.Phone;
                worksheet.Cell(currentRow, 4).Value = item.TelegramId;

                worksheet.Cell(currentRow, 5).Value = item.TotalVisits;
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 6).Value = item.TotalSpent;
                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00 ₴";
                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 7).Value = item.FirstVisit.HasValue ? item.FirstVisit.Value.ToString("dd.MM.yyyy") : "—";
                worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 8).Value = item.LastVisit.HasValue ? item.LastVisit.Value.ToString("dd.MM.yyyy") : "—";
                worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (i % 2 == 1)
                {
                    worksheet.Range(currentRow, 1, currentRow, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                currentRow++;
            }

            if (exportList.Count > 0)
            {
                var totalRow = currentRow;
                worksheet.Cell(totalRow, 2).Value = "ИТОГО:";
                worksheet.Cell(totalRow, 2).Style.Font.Bold = true;

                worksheet.Cell(totalRow, 5).FormulaA1 = $"SUM(E{headerRow + 1}:E{totalRow - 1})";
                worksheet.Cell(totalRow, 5).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(totalRow, 6).FormulaA1 = $"SUM(F{headerRow + 1}:F{totalRow - 1})";
                worksheet.Cell(totalRow, 6).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 6).Style.NumberFormat.Format = "#,##0.00 ₴";
                worksheet.Cell(totalRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range(totalRow, 1, totalRow, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range(totalRow, 1, totalRow, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            }

            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = 6;
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 20);
            worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 16);
            worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 18);

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportAllClientsToExcelAsync(Guid? filterOwnerId = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.Clients
                .AsNoTracking()
                .Where(c => c.TelegramId != "WALKIN")
                .Include(c => c.Owner)
                .Include(c => c.Appointments).ThenInclude(a => a.Service)
                .AsQueryable();

            if (filterOwnerId.HasValue && filterOwnerId.Value != Guid.Empty)
            {
                query = query.Where(c => c.OwnerId == filterOwnerId.Value);
            }

            var clients = await query
                .OrderByDescending(c => c.Appointments.Count)
                .ToListAsync();

            var exportList = clients.Select(c =>
            {
                var validAppointments = c.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .OrderBy(a => a.AppointmentDate)
                    .ToList();

                var totalSpent = validAppointments.Sum(a => a.Service?.Price ?? (a.OwnerProfit + a.MasterProfit));
                var firstVisit = validAppointments.FirstOrDefault()?.AppointmentDate;
                var lastVisit = validAppointments.LastOrDefault()?.AppointmentDate;

                return new ClientExportDto
                {
                    Name = c.Name,
                    Phone = c.Phone ?? "Не указан",
                    TelegramId = c.TelegramId,
                    BarbershopName = c.Owner?.BarbershopName ?? "Не указан",
                    OwnerName = c.Owner?.OwnerName ?? "Не указан",
                    TotalVisits = c.Appointments.Count(a => a.Status != AppointmentStatus.Cancelled),
                    TotalSpent = totalSpent,
                    FirstVisit = firstVisit,
                    LastVisit = lastVisit
                };
            }).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Клиенты платформы");

            worksheet.Cell(1, 1).Value = "Глобальная база клиентов — BarbershopB2B Platform";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

            worksheet.Cell(2, 1).Value = $"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm} (Всего клиентов: {exportList.Count})";
            worksheet.Cell(2, 1).Style.Font.FontSize = 10;
            worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");

            int headerRow = 4;
            string[] headers = new[]
            {
                "№",
                "Имя клиента",
                "Телефон",
                "Telegram ID",
                "Барбершоп",
                "Владелец",
                "Всего визитов",
                "Сумма покупок (₴)",
                "Первый визит",
                "Последний визит"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#B3B7DB");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            worksheet.Row(headerRow).Height = 24;

            int currentRow = headerRow + 1;
            for (int i = 0; i < exportList.Count; i++)
            {
                var item = exportList[i];
                worksheet.Cell(currentRow, 1).Value = i + 1;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 2).Value = item.Name;
                worksheet.Cell(currentRow, 3).Value = item.Phone;
                worksheet.Cell(currentRow, 4).Value = item.TelegramId;
                worksheet.Cell(currentRow, 5).Value = item.BarbershopName;
                worksheet.Cell(currentRow, 6).Value = item.OwnerName;

                worksheet.Cell(currentRow, 7).Value = item.TotalVisits;
                worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 8).Value = item.TotalSpent;
                worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0.00 ₴";
                worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 9).Value = item.FirstVisit.HasValue ? item.FirstVisit.Value.ToString("dd.MM.yyyy") : "—";
                worksheet.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 10).Value = item.LastVisit.HasValue ? item.LastVisit.Value.ToString("dd.MM.yyyy") : "—";
                worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (i % 2 == 1)
                {
                    worksheet.Range(currentRow, 1, currentRow, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                currentRow++;
            }

            if (exportList.Count > 0)
            {
                var totalRow = currentRow;
                worksheet.Cell(totalRow, 2).Value = "ИТОГО:";
                worksheet.Cell(totalRow, 2).Style.Font.Bold = true;

                worksheet.Cell(totalRow, 7).FormulaA1 = $"SUM(G{headerRow + 1}:G{totalRow - 1})";
                worksheet.Cell(totalRow, 7).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(totalRow, 8).FormulaA1 = $"SUM(H{headerRow + 1}:H{totalRow - 1})";
                worksheet.Cell(totalRow, 8).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 8).Style.NumberFormat.Format = "#,##0.00 ₴";
                worksheet.Cell(totalRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Range(totalRow, 1, totalRow, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range(totalRow, 1, totalRow, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            }

            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = 6;
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 20);
            worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 20);
            worksheet.Column(8).Width = Math.Max(worksheet.Column(8).Width, 18);

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
