namespace Backend.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetLink);
        Task SendSubscriptionExpirationWarningAsync(string toEmail, string ownerName, string barbershopName, DateTime nextPayment);
    }
}
