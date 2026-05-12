namespace JMAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public required string AppUserId { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public bool IsRead { get; set; } = false;
        public string? Type { get; set; }    // "job_status", "payment", "overdue"
        public int? EntityId { get; set; }   // jobId for deep linking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public AppUser? AppUser { get; set; }
    }
}
