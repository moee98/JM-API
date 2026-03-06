namespace JMAPI.Models
{
    public class CompanyDetails
    {
        public int Id { get; set; }
        public required string CompanyName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }

    }
}
