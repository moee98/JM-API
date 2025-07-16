
namespace JMAPI.Models
{
    public class VehicleInspection
    { 
        public int Id { get; set; } // Unique identifier for the inspection
        public Vehicle? Vehicle { get; set; } // Vehicle being inspected
        public DateTime InspectionDate { get; set; } // Date of the inspection
        public  User? User{ get; set; } // User who performed the inspection
        public int UserId { get; set; } // ID of the user who performed the inspection
        public required string InspectionResult { get; set; } // Result of the inspection (e.g., "Passed", "Failed")
        public required string Comments { get; set; } // Additional comments or notes from the inspection
        public required IList<string> PathToImages { get; set; } // List of image paths related to the inspection

    }
}
