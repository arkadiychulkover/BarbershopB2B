namespace Backend.DTOs
{
    public class AddBarberRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class UpdateBarberRequest
    {
        public Guid BarberId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class DeleteBarberRequest
    {
        public Guid BarberId { get; set; }
    }

    public class UpdateMasterUsernameRequest
    {
        public string? TelegramUsername { get; set; }
    }

    public class UpdateMasterProfileRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? TelegramUsername { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class MasterProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? PhotoUrl { get; set; }
        public decimal Rating { get; set; }
        public int ReviewsCount { get; set; }
        public bool IsActive { get; set; }
    }
}

