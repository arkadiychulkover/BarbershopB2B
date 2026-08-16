using Backend.Models.Enums;

namespace Backend.DTOs
{
    public class AdminStatsDto
    {
        public int TotalOwners { get; set; }
        public int ActiveOwners { get; set; }
        public int InactiveOwners { get; set; }
        public int TotalMasters { get; set; }
        public int ActiveMasters { get; set; }
        public int TotalClients { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AdminOwnerDto
    {
        public Guid Id { get; set; }
        public string OwnerName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string BarbershopName { get; set; }
        public string? BarbershopAddress { get; set; }
        public string? BarbershopDescription { get; set; }
        public string? BotToken { get; set; }
        public string? BotUsername { get; set; }
        public string Status { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime NextPayment { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MastersCount { get; set; }
        public int ClientsCount { get; set; }
        public int AppointmentsCount { get; set; }
        public decimal Turnover { get; set; }
    }

    public class UpdateOwnerSubscriptionRequest
    {
        public bool? IsActive { get; set; }
        public bool? IsBlocked { get; set; }
        public DateTime? NextPayment { get; set; }
        public int? ExtendDays { get; set; }
    }

    public class UpdateOwnerProfileRequest
    {
        public string? OwnerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BarbershopName { get; set; }
        public string? BarbershopAddress { get; set; }
        public string? BarbershopDescription { get; set; }
        public string? BotToken { get; set; }
        public string? BotUsername { get; set; }
    }

    public class AdminMasterDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string BarbershopName { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; }
        public decimal Rating { get; set; }
        public int ReviewsCount { get; set; }
        public int AppointmentsCount { get; set; }
    }

    public class UpdateAdminMasterRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? TelegramId { get; set; }
        public string? TelegramUsername { get; set; }
        public bool? IsActive { get; set; }
    }

    public class AdminClientDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string BarbershopName { get; set; }
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string TelegramId { get; set; }
        public int AppointmentsCount { get; set; }
    }

    public class AdminAppointmentDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string BarbershopName { get; set; }
        public string MasterName { get; set; }
        public string ClientName { get; set; }
        public string? ClientPhone { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentEndDate { get; set; }
        public AppointmentStatus Status { get; set; }
    }

    public class CreateAdminRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class AdminDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
