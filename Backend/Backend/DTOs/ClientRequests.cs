namespace Backend.DTOs
{
    public record BookAppointmentRequest(Guid MasterId, Guid ServiceId, DateTime AppointmentDate);

    public record UpdateClientPhoneRequest(string Phone);

    public class ClientProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string TelegramId { get; set; }
    }

    public class ClientMasterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? TelegramId { get; set; }
        public string? Username { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Description { get; set; }
    }

    public class ClientServiceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string? Description { get; set; }
    }
}
