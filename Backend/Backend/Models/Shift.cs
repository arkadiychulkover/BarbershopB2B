namespace Backend.Models
{
    public class Shift
    {
        public Guid Id { get; set; }
        public Guid MasterId { get; set; }
        public Master Master { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        /// <summary>Начало перерыва (обед / пауза). Опционально.</summary>
        public TimeOnly? BreakStartTime { get; set; }

        /// <summary>Конец перерыва. Опционально.</summary>
        public TimeOnly? BreakEndTime { get; set; }

        /// <summary>Перерыв / буферное время после каждой записи в минутах (например, 15 мин). 0 — без перерыва.</summary>
        public int BreakDurationMinutes { get; set; } = 0;
    }
}
