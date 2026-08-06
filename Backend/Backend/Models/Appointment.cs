using Backend.Models.Enums;

namespace Backend.Models
{
    public class Appointment
    {
        public Guid Id { get; set; }

        public Guid MasterId { get; set; }
        public Master Master { get; set; }

        public Guid ClientId { get; set; }
        public Client Client { get; set; }

        public Guid ServiceId { get; set; }
        public Service Service { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentEndDate { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        public decimal MasterProfit { get; set; }
        public decimal OwnerProfit { get; set; }

        public bool DepositPaid { get; set; } = false;
        public decimal? DepositAmount { get; set; }

        public string? PhotoResultUrl { get; set; }
        public string? ResultNote { get; set; }
    }
}