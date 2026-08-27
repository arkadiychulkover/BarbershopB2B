using System.Net;

namespace Backend.Models
{
    public class Master
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public string? TelegramId { get; set; }
        public string? TelegramUsername { get; set; }

        public Guid OwnerId { get; set; }
        public BarbershopOwner Owner { get; set; }

        public List<Service> Services { get; set; }
        public List<Shift> Shifts { get; set; }
        public List<Appointment> Appointments { get; set; }
        public List<Review> Reviews { get; set; }
        public List<MasterVacation> Vacations { get; set; } = new();

        public decimal CommissionsProfit { get; set; }

        public decimal Rating { get; set; } = 0;
        public int ReviewsCount { get; set; } = 0;

        public decimal MoneyInDay { get; set; } = 0;
        public decimal MoneyInWeek { get; set; } = 0;
        public decimal MoneyInMonth { get; set; } = 0;

        [System.Text.Json.Serialization.JsonIgnore]
        public IPAddress Ip { get; set; }
    }
}