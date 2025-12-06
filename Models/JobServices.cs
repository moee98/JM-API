namespace JMAPI.Models
{
    public class JobServices
    {
        public int id { get; set; } // Unique identifier for the job service
        public int JobId { get; set; } // ID of the job associated with the service
        public  Job? Job { get; set; } // Navigation property to the Job entity
        public int ServiceId { get; set; } // ID of the service being performed
        public  Service? Service { get; set; } // Navigation property to the Service entity
        public float Price { get; set; }
    }
}
 