using Microsoft.AspNetCore.Http;

namespace JMAPI.Models
{
    public sealed class AttachmentUploadRequest
    {
        public List<IFormFile> Files { get; set; } = [];
    }
}
