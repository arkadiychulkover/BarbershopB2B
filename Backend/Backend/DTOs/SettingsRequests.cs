namespace Backend.DTOs
{
    public class UpdateSettingsRequest
    {
        public string BarbershopName { get; set; }
        public string BarbershopAddress { get; set; }
        public string BarbershopDescription { get; set; }
        public string OwnerName { get; set; }
        public string TimeZone { get; set; }
        public string? PhoneNumber { get; set; }
        public string? TelegramId { get; set; }
        public string? BotToken { get; set; }
        public string? BotUsername { get; set; }
        public int ReminderHoursBefore { get; set; }
        /// <summary>Win-back: через сколько дней без визита слать напоминание. 0 = откл.</summary>
        public int WinBackDays { get; set; }
        public decimal MasterFee { get; set; }
        public string WalletAddress { get; set; }
    }

    public class SettingsResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string BarbershopName { get; set; }
        public string BarbershopAddress { get; set; }
        public string BarbershopDescription { get; set; }
        public string OwnerName { get; set; }
        public string TimeZone { get; set; }
        public string? PhoneNumber { get; set; }
        public string? TelegramId { get; set; }
        public string? BotToken { get; set; }
        public string? BotUsername { get; set; }
        public int ReminderHoursBefore { get; set; }
        /// <summary>Win-back: через сколько дней без визита слать напоминание. 0 = откл.</summary>
        public int WinBackDays { get; set; }
        public decimal MasterFee { get; set; }
        public string WalletAddress { get; set; }
        public bool IsSubscribed { get; set; }
        public string Status { get; set; }
        public DateTime NextPayment { get; set; }
        public DateTime LastPayment { get; set; }
    }

    public class UpdateWalletRequest
    {
        public string WalletAddress { get; set; } = string.Empty;
    }
}
