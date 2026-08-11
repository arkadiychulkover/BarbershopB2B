namespace Backend.Models
{
    public class ServiceName
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid OwnerId { get; set; }
        public BarbershopOwner Owner { get; set; }
    }
}
