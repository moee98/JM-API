using System.Text.Json.Serialization;

namespace JMAPI.Models
{
    public class ExpenseItemAttachment
    {
        public int Id { get; set; }
        public int ExpenseItemId { get; set; }
        [JsonIgnore]
        public ExpenseItem? ExpenseItem { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        [JsonIgnore]
        public byte[] Data { get; set; } = [];
        public DateTime UploadedAt { get; set; }
    }
}
