using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JMAPI.Models
{
    public class Job
    {
        [Key]
        
        public int Id { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string Status { get; set; } // e.g., "Pending", "InProgress", "Completed
        public required string Priority { get; set; } // e.g., "Low", "Medium", "High"
        public  DateTime DueDate { get; set; } // When the job is due
        public required string Notes { get; set; } // Additional notes or comments
        public bool IsActive { get; set; } // Indicates if the job is currently active
        public bool Paid { get; set; } // Indicates if the job has been paid for
        public required string PaymentMethod { get; set; } // Method of payment (e.g., "Credit Card", "Cash")
        public int ServiceCharge { get; set; } // Service charge for the job
        public Vehicle? Vehicle { get; set; }// Vehicle associated with the job
        public int VehicleId { get; set; }
        //public  VehicleInspection? VehicleInspection { get; set; }   // Vehicle inspection associated with the job
        public int CustomerId { get; set; } // ID of the customer associated with the job

        public  Customer? Customer { get; set; }

        public  User? CreatedByUser { get; set; } // User who created the job
        public int CreatedByUserId { get; set; }
        [NotMapped]
        public IList<Service>? Services { get; set; } = new List<Service>(); // List of services associated with the job

    }
}
