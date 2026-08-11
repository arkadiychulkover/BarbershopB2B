using Backend.Models.Enums;

namespace Backend.DTOs
{
    internal class AppointmentDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public Guid MasterId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentEndDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public bool ReminderSent { get; set; }
        public Guid ServiceId { get; set; }
    }
}