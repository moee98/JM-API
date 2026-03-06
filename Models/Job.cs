using JMAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Job
{
    [Key]
    public int Id { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string Status { get; set; }
    public DateTime DueDate { get; set; }
    public required string Notes { get; set; }
    public bool IsActive { get; set; }
    public bool Paid { get; set; }
    public IList<PaymentMethods>? PaymentMethod { get; set; } = new List<PaymentMethods>();
    public int ServiceCharge { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? VehicleInspectionId { get; set; }
    public VehicleInspection? VehicleInspection { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    [ForeignKey("AppUser")]
    public string? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
    public IList<JobServices> JobServices { get; set; } = new List<JobServices>();
    [NotMapped]
    public IList<Service>? Services { get; set; }
}
