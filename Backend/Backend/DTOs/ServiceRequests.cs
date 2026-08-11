using System;

namespace Backend.DTOs
{
    public class ServiceNameDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class CreateServiceNameRequest
    {
        public string Name { get; set; }
    }

    public class UpdateServiceNameRequest
    {
        public string Name { get; set; }
    }

    public class MasterServiceDto
    {
        public Guid ServiceNameId { get; set; }
        public string Name { get; set; }
        
        // These are null if the master hasn't configured this service yet
        public Guid? ServiceId { get; set; }
        public decimal? Price { get; set; }
        public int? Duration { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateMasterServiceRequest
    {
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
    }
}
