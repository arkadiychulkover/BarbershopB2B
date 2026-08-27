namespace Backend.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetLink);
    }
}
