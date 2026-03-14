using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JMAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleInspectionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleInspectionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleInspectionResponse>>> GetVehicleInspection()
        {
            var inspections = await _context.VehicleInspection
                .Include(x => x.Attachments)
                .ToListAsync();

            return Ok(inspections.Select(MapVehicleInspection));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VehicleInspectionResponse>> GetVehicleInspection(int id)
        {
            var vehicleInspection = await _context.VehicleInspection
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (vehicleInspection == null)
            {
                return NotFound();
            }

            return Ok(MapVehicleInspection(vehicleInspection));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicleInspection(int id, VehicleInspection vehicleInspection)
        {
            if (id != vehicleInspection.Id)
            {
                return BadRequest();
            }

            var existing = await _context.VehicleInspection.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.VehicleId = vehicleInspection.VehicleId;
            existing.InspectionDate = vehicleInspection.InspectionDate;
            existing.InspectionResult = vehicleInspection.InspectionResult;
            existing.Comments = vehicleInspection.Comments;
            existing.PathToImages = vehicleInspection.PathToImages;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<VehicleInspectionResponse>> PostVehicleInspection(VehicleInspection vehicleInspection)
        {
            _context.VehicleInspection.Add(vehicleInspection);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehicleInspection), new { id = vehicleInspection.Id }, MapVehicleInspection(vehicleInspection));
        }

        [HttpPost("{id:int}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadVehicleInspectionAttachments(int id, [FromForm] AttachmentUploadRequest request, CancellationToken cancellationToken)
        {
            if (request.Files == null || request.Files.Count == 0)
            {
                return BadRequest("At least one image or PDF file is required.");
            }

            var invalidFiles = request.Files
                .Where(file => !AttachmentFileHelper.IsSupported(file))
                .Select(file => file.FileName)
                .ToList();

            if (invalidFiles.Count > 0)
            {
                return BadRequest($"Unsupported files: {string.Join(", ", invalidFiles)}. Only images and PDFs are allowed.");
            }

            var vehicleInspection = await _context.VehicleInspection
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (vehicleInspection == null)
            {
                return NotFound($"Vehicle inspection {id} not found.");
            }

            foreach (var file in request.Files)
            {
                vehicleInspection.Attachments.Add(new VehicleInspectionAttachment
                {
                    FileName = file.FileName,
                    ContentType = AttachmentFileHelper.NormalizeContentType(file),
                    FileSize = file.Length,
                    Data = await AttachmentFileHelper.ReadBytesAsync(file, cancellationToken),
                    UploadedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(vehicleInspection.Attachments.Select(attachment => MapAttachment(attachment, id, nameof(GetVehicleInspectionAttachmentContent))));
        }

        [HttpGet("{id:int}/attachments/{attachmentId:int}")]
        public async Task<IActionResult> GetVehicleInspectionAttachmentContent(int id, int attachmentId, CancellationToken cancellationToken)
        {
            var attachment = await _context.VehicleInspectionAttachments
                .FirstOrDefaultAsync(x => x.VehicleInspectionId == id && x.Id == attachmentId, cancellationToken);

            if (attachment == null)
            {
                return NotFound();
            }

            return File(attachment.Data, attachment.ContentType, attachment.FileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicleInspection(int id)
        {
            var vehicleInspection = await _context.VehicleInspection.FindAsync(id);
            if (vehicleInspection == null)
            {
                return NotFound();
            }

            _context.VehicleInspection.Remove(vehicleInspection);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private VehicleInspectionResponse MapVehicleInspection(VehicleInspection inspection)
        {
            var attachments = inspection.Attachments?
                .Select(attachment => MapAttachment(attachment, inspection.Id, nameof(GetVehicleInspectionAttachmentContent)))
                .ToArray()
                ?? [];

            return new VehicleInspectionResponse
            {
                Id = inspection.Id,
                VehicleId = inspection.VehicleId,
                InspectionDate = inspection.InspectionDate,
                InspectionResult = inspection.InspectionResult,
                Comments = inspection.Comments,
                PathToImages = inspection.PathToImages.ToArray(),
                Attachments = attachments
            };
        }

        private AttachmentSummaryResponse MapAttachment(VehicleInspectionAttachment attachment, int inspectionId, string actionName)
        {
            return new AttachmentSummaryResponse
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt,
                DownloadUrl = Url.Action(actionName, values: new { id = inspectionId, attachmentId = attachment.Id }) ?? string.Empty
            };
        }
    }
}
