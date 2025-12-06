using Microsoft.AspNetCore.Identity;
namespace JMAPI.Models
{
    public class AppUser : IdentityUser
    {
        public string? Name { get; set; }
        public bool? Active { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public ICollection<Job>? Jobs { get; set; }
        //public ICollection<Job>? JobsCreated { get; set; }
    }
}
