using JMAPI.Database;
using JMAPI.Interfaces;
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
    public class ExpenseItemsController : ControllerBase
    {
        private readonly IExpenseItemsService _expenseItem;
        private readonly AppDbContext _context;

        public ExpenseItemsController(IExpenseItemsService expenseItemService, AppDbContext context)
        {
            _expenseItem = expenseItemService;
            _context = context;
        }

        // GET: api/ExpenseItems?from=2026-01-01&to=2026-01-31
        [HttpGet]
        public async Task<IActionResult> GetExpenseItems([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var items = await _expenseItem.GetAllAsync(from, to);
            return Ok(items.Select(MapExpenseItem));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetExpenseItem(int id)
        {
            var expenseItem = await _expenseItem.GetByIdAsync(id);

            if (expenseItem == null)
            {
                return NotFound();
            }

            return Ok(MapExpenseItem(expenseItem));
        }

        [HttpGet("category")]
        public async Task<IActionResult> GetExpenseCategories()
        {
            var items = await _expenseItem.GetExpenseCategories();
            if (items == null || !items.Any())
            {
                return NotFound("No category found.");
            }

            return Ok(items);
        }

        [HttpPost("category")]
        public async Task<IActionResult> CreateExpenseCategory([FromBody] ExpenseCategory request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Category name is required.");
            }

            var created = await _expenseItem.CreateExpenseCategory(request.Name.Trim());
            return Ok(created);
        }

        [HttpPut("{id}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateExpenseItem(int id, [FromBody] ExpenseItem request)
        {
            if (request == null) return BadRequest("Request body is required.");
            if (id != request.Id) return BadRequest("Route id does not match payload id.");

            var updated = await _expenseItem.UpdateAsync(request);
            if (!updated) return NotFound($"Expense Item {id} not found.");

            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> PostExpenseItem(ExpenseItem expenseItem)
        {
            var created = await _expenseItem.CreateAsync(expenseItem);
            return CreatedAtAction(nameof(GetExpenseItem), new { id = created.Id }, MapExpenseItem(created));
        }

        [HttpPost("{id:int}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadExpenseItemAttachments(int id, [FromForm] AttachmentUploadRequest request, CancellationToken cancellationToken)
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

            var expenseItem = await _context.ExpenseItems
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (expenseItem == null)
            {
                return NotFound($"Expense Item {id} not found.");
            }

            foreach (var file in request.Files)
            {
                expenseItem.Attachments.Add(new ExpenseItemAttachment
                {
                    FileName = file.FileName,
                    ContentType = AttachmentFileHelper.NormalizeContentType(file),
                    FileSize = file.Length,
                    Data = await AttachmentFileHelper.ReadBytesAsync(file, cancellationToken),
                    UploadedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(expenseItem.Attachments.Select(attachment => MapAttachment(attachment, id, nameof(GetExpenseItemAttachmentContent))));
        }

        [HttpGet("{id:int}/attachments/{attachmentId:int}")]
        public async Task<IActionResult> GetExpenseItemAttachmentContent(int id, int attachmentId, CancellationToken cancellationToken)
        {
            var attachment = await _context.ExpenseItemAttachments
                .FirstOrDefaultAsync(x => x.ExpenseItemId == id && x.Id == attachmentId, cancellationToken);

            if (attachment == null)
            {
                return NotFound();
            }

            return File(attachment.Data, attachment.ContentType, attachment.FileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpenseItem(int id)
        {
            var res = await _expenseItem.DeleteAsync(id);
            if (!res)
            {
                return NotFound();
            }

            return NoContent();
        }

        private ExpenseItemResponse MapExpenseItem(ExpenseItem item)
        {
            var attachments = item.Attachments?
                .Select(attachment => MapAttachment(attachment, item.Id, nameof(GetExpenseItemAttachmentContent)))
                .ToArray()
                ?? [];

            return new ExpenseItemResponse
            {
                Id = item.Id,
                Description = item.Description,
                Amount = item.Amount,
                DateIncurred = item.DateIncurred,
                ExpenseCategoryId = item.ExpenseCategoryId,
                ReceiptImagePath = item.ReceiptImagePath,
                IsReimbursed = item.IsReimbursed,
                PaymentMethod = item.PaymentMethod,
                PaidTo = item.PaidTo,
                Attachments = attachments
            };
        }

        private AttachmentSummaryResponse MapAttachment(ExpenseItemAttachment attachment, int expenseItemId, string actionName)
        {
            return new AttachmentSummaryResponse
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt,
                DownloadUrl = Url.Action(actionName, values: new { id = expenseItemId, attachmentId = attachment.Id }) ?? string.Empty
            };
        }
    }
}
