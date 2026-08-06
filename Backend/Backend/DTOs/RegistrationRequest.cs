using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class RegistrationRequest
    {
        [Required]
        public string OwnerName { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string TelegramId { get; set; }

        [Required]
        public string BarbershopName { get; set; }

        [Required]
        public string BarbershopAddress { get; set; }

        public string? BarbershopDescription { get; set; }

        [Required]
        public string BotToken { get; set; }

        [Required]
        public string BotUsername { get; set; }

        public string TimeZone { get; set; } = "Europe/Kyiv";
        
        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }
    }
}
