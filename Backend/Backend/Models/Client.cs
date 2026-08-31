namespace Backend.Models
{
    public class Client
    {
        public Guid Id { get; set; }
        public string TelegramId { get; set; }
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public bool IsBlacklisted { get; set; } = false;
        public bool HasBlocked { get; set; } = false;

        /// <summary>Дата последнего отправленного win-back сообщения</summary>
        public DateTime? LastWinBackSentAt { get; set; }

        public Guid OwnerId { get; set; }
        public BarbershopOwner Owner { get; set; }

        public List<Appointment> Appointments { get; set; }
    }
}
