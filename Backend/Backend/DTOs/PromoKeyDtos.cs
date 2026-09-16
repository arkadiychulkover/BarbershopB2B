using System.ComponentModel.DataAnnotations;
using Backend.Models.Enums;

namespace Backend.DTOs
{
    public class CreatePromoKeyRequest
    {
        [Range(1, 3650, ErrorMessage = "Количество дней должно быть от 1 до 3650.")]
        public int Days { get; set; } = 30;
    }

    public class UpdatePromoKeyRequest
    {
        [Range(1, 3650, ErrorMessage = "Количество дней должно быть от 1 до 3650.")]
        public int Days { get; set; }
    }

    public class RedeemPromoKeyRequest
    {
        [Required(ErrorMessage = "Ключ обязателен для ввода.")]
        public string Key { get; set; } = string.Empty;
    }

    public class AdminPromoKeyDto
    {
        public Guid Id { get; set; }
        public int Days { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public Guid? UsedByOwnerId { get; set; }
        public string? BarbershopName { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerEmail { get; set; }
    }

    public class CreatedPromoKeyResultDto
    {
        public Guid Id { get; set; }
        public string RawKey { get; set; } = string.Empty;
        public int Days { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
