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
    }
}
