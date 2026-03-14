using System.Text.Json.Serialization;

namespace JMAPI.Models
{
    public class VehicleInspection
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        public DateTime InspectionDate { get; set; }
        public required string InspectionResult { get; set; }
        public required string Comments { get; set; }
        public IList<string> PathToImages { get; set; } = [];
        [JsonIgnore]
        public IList<VehicleInspectionAttachment> Attachments { get; set; } = [];
    }
}
