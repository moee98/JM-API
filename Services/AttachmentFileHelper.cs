using Microsoft.AspNetCore.Http;

namespace JMAPI.Services
{
    internal static class AttachmentFileHelper
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".webp",
            ".pdf"
        };

        public static bool IsSupported(IFormFile file)
        {
            if (file.Length <= 0)
            {
                return false;
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(file.ContentType)
                || file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                || file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<byte[]> ReadBytesAsync(IFormFile file, CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream.ToArray();
        }

        public static string NormalizeContentType(IFormFile file)
        {
            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                return file.ContentType;
            }

            return Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/octet-stream";
        }
    }
}
