namespace Backend.Models
{
    public class Service
    {
        public Guid Id { get; set; }

        public Guid MasterId { get; set; }
        public Master Master { get; set; }

        public Guid ServiceNameId { get; set; }
        public ServiceName ServiceName { get; set; }

        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public bool IsActive { get; set; } = true;
    }
}