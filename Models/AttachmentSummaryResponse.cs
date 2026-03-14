namespace JMAPI.Models
{
    public sealed class AttachmentSummaryResponse
    {
        public int Id { get; init; }
        public required string FileName { get; init; }
        public required string ContentType { get; init; }
        public long FileSize { get; init; }
        public DateTime UploadedAt { get; init; }
        public required string DownloadUrl { get; init; }
    }
}
