namespace JMAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public UserRole? Role { get; set; } // e.g., Admin, Customer, etc.
        public int RoleId { get; set; }

        // Additional properties can be added as needed
    }
}
