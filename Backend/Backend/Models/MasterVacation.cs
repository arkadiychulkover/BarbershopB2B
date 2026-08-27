namespace Backend.Models
{
    /// <summary>Отпуск или больничный мастера — диапазон дат, в которые он недоступен для записи.</summary>
    public class MasterVacation
    {
        public Guid Id { get; set; }

        public Guid MasterId { get; set; }
        public Master Master { get; set; }

        /// <summary>Начало периода недоступности (UTC, только дата)</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Конец периода недоступности (UTC, только дата, включительно)</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Причина: «Отпуск», «Больничный», и т.д.</summary>
        public string? Reason { get; set; }
    }
}
