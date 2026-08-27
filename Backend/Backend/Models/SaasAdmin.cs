namespace Backend.Models
{
    public class SaasAdmin
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string PasswordSalt { get; set; }
        public string PasswordHash { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpires { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
