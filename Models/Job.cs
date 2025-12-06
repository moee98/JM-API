using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JMAPI.Models
{
    public class Job
    {
        [Key]
        
        public int Id { get; set; }
        public  string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string Status { get; set; } // e.g., "Pending", "InProgress", "Completed
        public  DateTime DueDate { get; set; } // When the job is due
        public required string Notes { get; set; } // Additional notes or comments
        public bool IsActive { get; set; } // Indicates if the job is currently active
        public bool Paid { get; set; } // Indicates if the job has been paid for
        public IList<PaymentMethods>? PaymentMethod { get; set; } = new List<PaymentMethods>(); // Method of payment (e.g., "Credit Card", "Cash")
        public int ServiceCharge { get; set; } // Service charge for the job
        public Vehicle? Vehicle { get; set; }// Vehicle associated with the job
        public int VehicleId { get; set; }
        public  VehicleInspection? VehicleInspection { get; set; }   // Vehicle inspection associated with the job
        public int? VehicleInspectionId { get; set; }
        public int CustomerId { get; set; } // ID of the customer associated with the job
        public  Customer? Customer { get; set; }
        public  AppUser? AppUser { get; set; } // User who created the job
        [ForeignKey("AppUser")]
        public string? AppUserId { get; set; }
         [NotMapped]
        public IList<JobServices> JobServices { get; set; } 
        
        [NotMapped]
        public IList<Service>? Services { get; set; }  // List of services associated with the job

    }
}
