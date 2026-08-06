namespace Backend.Models
{
    public class Shift
    {
        public Guid Id { get; set; }
        public Guid MasterId { get; set; }
        public Master Master { get; set; }

        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
