using Microsoft.AspNetCore.Identity;
namespace JMAPI.Models
{
    public class AppUser : IdentityUser
    {
        public string? Name { get; set; }
        public bool? Active { get; set; }
        // Sessions live in the RefreshTokens table (one row per device), not in
        // a single column here - see Models/RefreshToken.cs.
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<Job>? Jobs { get; set; }
        //public ICollection<Job>? JobsCreated { get; set; }
    }
}
