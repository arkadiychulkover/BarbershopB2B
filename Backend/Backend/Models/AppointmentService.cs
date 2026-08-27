namespace Backend.Models
{
    /// <summary>Связь записи (Appointment) с дополнительными услугами — для мульти-услуг в одной записи.</summary>
    public class AppointmentService
    {
        public Guid Id { get; set; }

        public Guid AppointmentId { get; set; }
        public Appointment Appointment { get; set; }

        public Guid ServiceId { get; set; }
        public Service Service { get; set; }

        /// <summary>Цена на момент бронирования (фиксируется, чтобы не менялась при изменении прайса)</summary>
        public decimal Price { get; set; }

        /// <summary>Длительность услуги на момент бронирования (в минутах)</summary>
        public int Duration { get; set; }
    }
}
