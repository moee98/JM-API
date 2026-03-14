using System.Text.Json.Serialization;

namespace JMAPI.Models
{
    public class VehicleInspectionAttachment
    {
        public int Id { get; set; }
        public int VehicleInspectionId { get; set; }
        [JsonIgnore]
        public VehicleInspection? VehicleInspection { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        [JsonIgnore]
        public byte[] Data { get; set; } = [];
        public DateTime UploadedAt { get; set; }
    }
}
