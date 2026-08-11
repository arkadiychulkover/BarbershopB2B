namespace Backend.DTOs
{
    public record BookAppointmentRequest(Guid MasterId, Guid ServiceId, DateTime AppointmentDate);

    public class ClientMasterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
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
