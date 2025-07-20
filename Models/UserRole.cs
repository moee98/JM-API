using Microsoft.AspNetCore.Identity;
namespace JMAPI.Models
{
    public class UserRole
    {
        public int Id { get; set; }
        // Unique identifier for the user role
        public required string RoleName { get; set; } // Name of the role (e.g., "Admin", "User", "Manager")
        public required string Description { get; set; } // Description of the role
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp when the role was created
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Timestamp when the role was last updated
        public bool IsActive { get; set; } = true; // Indicates if the role is currently active
    }
}
