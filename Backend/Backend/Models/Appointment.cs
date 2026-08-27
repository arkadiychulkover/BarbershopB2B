
using Backend.Models.Enums;

namespace Backend.Models
{
    public class Appointment
    {
        public Guid Id { get; set; }

        public Guid MasterId { get; set; }
        public Master Master { get; set; }

        public Guid ClientId { get; set; }
        public Client Client { get; set; }

        /// <summary>Основная услуга (первичная, для обратной совместимости)</summary>
        public Guid ServiceId { get; set; }
        public Service Service { get; set; }

        /// <summary>Дополнительные услуги в записи (комплекс)</summary>
        public List<AppointmentService> AdditionalServices { get; set; } = new();

        public Review? Review { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentEndDate { get; set; }
        
        public bool ReminderSent { get; set; } = false;
        public DateTime? ReminderTime { get; set; }

        /// <summary>Клиент подтвердил визит через Telegram-бота.</summary>
        public bool IsConfirmed { get; set; } = false;

        /// <summary>Сообщение с кнопкой подтверждения уже было отправлено.</summary>
        public bool ConfirmationSent { get; set; } = false;

        /// <summary>ID сообщения Telegram для редактирования после ответа клиента.</summary>
        public int? ConfirmationMessageId { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        public decimal MasterProfit { get; set; }
        public decimal OwnerProfit { get; set; }

        public bool DepositPaid { get; set; } = false;
        public decimal? DepositAmount { get; set; }

        public string? PhotoResultUrl { get; set; }
        public string? ResultNote { get; set; }
    }
}