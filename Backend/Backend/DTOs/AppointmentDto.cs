using Backend.Models.Enums;

namespace Backend.DTOs
{
    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientTelegramId { get; set; }
        public Guid MasterId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentEndDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public bool ReminderSent { get; set; }
        public DateTime? ReminderTime { get; set; }
        public Guid ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public string? PhotoResultUrl { get; set; }
        public string? ResultNote { get; set; }
        public string? Comment => ResultNote;
    }
}