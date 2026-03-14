namespace JMAPI.Models
{
    public sealed class VehicleInspectionResponse
    {
        public int Id { get; init; }
        public int VehicleId { get; init; }
        public DateTime InspectionDate { get; init; }
        public required string InspectionResult { get; init; }
        public required string Comments { get; init; }
        public IReadOnlyList<string> PathToImages { get; init; } = [];
        public IReadOnlyList<AttachmentSummaryResponse> Attachments { get; init; } = [];
    }
}
