namespace JMAPI.Models
{
    public class Service 
    {
        public int Id { get; set; }  
        public required string Name { get; set; } // Name of the service
        public required string Description { get; set; } // Description of the service
        public float EstimatedPrice { get; set; } // Estimated price for the service
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp when the service was created
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Timestamp when the service was last updated
        public bool IsActive { get; set; } = true; // Indicates if the service is currently active

    }
}
