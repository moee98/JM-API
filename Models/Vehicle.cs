namespace JMAPI.Models
{
    public class Vehicle
    {
        public int Id { get; set; } // Unique identifier for the vehicle
        public required string Make { get; set; } // Manufacturer of the vehicle (e.g., "Toyota", "Ford")
        public required string Model { get; set; } // Model of the vehicle (e.g., "Camry", "F-150")
        
        public required string LicensePlate { get; set; } // Vehicle's license plate number
        public required string Colour { get; set; } // Color of the vehicle
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp when the vehicle was added
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Timestamp when the vehicle details were last updated
        public bool IsActive { get; set; } = true; // Indicates if the vehicle is currently active or in use
    }
}
