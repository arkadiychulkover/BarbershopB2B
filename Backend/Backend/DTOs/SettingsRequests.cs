namespace Backend.DTOs
{
    public class UpdateSettingsRequest
    {
        public string BarbershopName { get; set; }
        public string BarbershopAddress { get; set; }
        public string BarbershopDescription { get; set; }
        public string OwnerName { get; set; }
        public string TimeZone { get; set; }
        public string? LogoUrl { get; set; }
        public string? BrandColor { get; set; }
        public int ReminderHoursBefore { get; set; }
        public bool DepositEnabled { get; set; }
        public decimal? DepositPercent { get; set; }
        public decimal MasterFee { get; set; }
        public string WalletAddress { get; set; }
    }

    public class SettingsResponse
    {
        public string BarbershopName { get; set; }
        public string BarbershopAddress { get; set; }
        public string BarbershopDescription { get; set; }
        public string OwnerName { get; set; }
        public string TimeZone { get; set; }
        public string? LogoUrl { get; set; }
        public string? BrandColor { get; set; }
        public int ReminderHoursBefore { get; set; }
        public bool DepositEnabled { get; set; }
        public decimal? DepositPercent { get; set; }
        public decimal MasterFee { get; set; }
        public string WalletAddress { get; set; }
        
        // Read-only settings / statuses
        public bool IsSubscribed { get; set; }
        public string Status { get; set; }
    }
}
