namespace Backend.Models
{
    public class Tranzaction
    {
        public Guid Id { get; set; }
        
        public Guid OwnerId { get; set; }
        public BarbershopOwner Owner { get; set; }

        public DateTime Time { get; set; }

        public decimal Amount { get; set; }
    }
}
