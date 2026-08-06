namespace Backend.Models
{
    public class Review
    {
        public Guid Id { get; set; }

        public Guid AppointmentId { get; set; }
        public Appointment Appointment { get; set; }

        public Guid MasterId { get; set; }
        public Master Master { get; set; }

        public Guid ClientId { get; set; }
        public Client Client { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}