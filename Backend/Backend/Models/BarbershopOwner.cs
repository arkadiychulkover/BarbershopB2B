using Backend.Models.Enums;

namespace Backend.Models
{
    public class BarbershopOwner
    {
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; }
        public string TelegramId { get; set; }
        public string BotToken { get; set; }
        public string BotUsername { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public string WalletAddress { get; set; }

        public string BarbershopName { get; set; }
        public string BarbershopAddress { get; set; }
        public string BarbershopDescription { get; set; }
        public string OwnerName { get; set; }

        public string TimeZone { get; set; } = "Europe/Kyiv";
        public string? LogoUrl { get; set; }
        public string? BrandColor { get; set; }

        public int ReminderHoursBefore { get; set; } = 2;
        public bool DepositEnabled { get; set; } = false;
        public decimal? DepositPercent { get; set; }

        public List<Master> Masters { get; set; }
        public List<Client> Clients { get; set; }
        public List<Tranzaction> Tranzactions { get; set; }

        public OwnerStatus Status { get; set; } = OwnerStatus.Pending;
        public bool IsBlocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime PayedAt { get; set; }
        public DateTime NextPayment { get; set; }
        public DateTime LastPayment { get; set; }
        public bool IsSubscribed => NextPayment > DateTime.UtcNow;

        public decimal MasterFee { get; set; }
    }
}
