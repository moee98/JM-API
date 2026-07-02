using JMAPI.Interfaces;
using JMAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace JMAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly IEmailService _emailService;

        public JobController(IJobService jobService, IEmailService emailService)
        {
            _jobService = jobService;
            _emailService = emailService;
        }

        // GET: api/<JobController>?page=1&pageSize=20&search=ford&status=Pending&paid=false
        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] bool? paid = null)
        {
            var result = await _jobService.GetAllAsync(page, pageSize, search, status, paid);
            return Ok(result);
        }

        // GET: api/<JobController>/outstanding
        [HttpGet("outstanding")]
        public async Task<IActionResult> GetOutstandingAsync()
        {
            var jobs = await _jobService.GetOutstandingAsync();
            return Ok(jobs);
        }

        // GET api/<JobController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetId(int id)
        {
            try
            {
                var job = await _jobService.GetByIdWithDetailsAsync(id);
                if (job == null)
                    return NotFound();

                job.AppUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                return Ok(job);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST api/<JobController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Job job)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (job == null)
            {
                return BadRequest("Job cannot be null.");
            }

            // Always derive the owner from the authenticated JWT — never trust a
            // client-supplied appUserId as that would allow privilege escalation.
            job.AppUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            try
            {
                var created = await _jobService.CreateAsync(job);
                if (created == null)
                {
                    return StatusCode(500, "Failed to create job.");
                }

                return StatusCode(200, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message + ex.InnerException);
            }
        }

        // PUT api/<JobController>/5
        [HttpPut("{id}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateJob(int id, [FromBody] Job request)
        {
            if (request == null) return BadRequest("Request body is required.");

            var updated = await _jobService.UpdateAsync(id, request);
            if (!updated) return NotFound($"Job {id} not found.");

            return NoContent();
        }

        // POST api/<JobController>/5/send-invoice
        [HttpPost("{id}/send-invoice")]
        public async Task<IActionResult> SendInvoice(int id)
        {
            try
            {
                await _emailService.SendInvoiceAsync(id);
                return Ok("Invoice sent successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST api/<JobController>/5/send-reminder
        [HttpPost("{id}/send-reminder")]
        public async Task<IActionResult> SendReminder(int id)
        {
            try
            {
                await _emailService.SendPaymentReminderAsync(id);
                return Ok("Payment reminder sent successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST api/<JobController>/5/send-booking-confirmation
        [HttpPost("{id}/send-booking-confirmation")]
        public async Task<IActionResult> SendBookingConfirmation(int id)
        {
            try
            {
                await _emailService.SendBookingConfirmationAsync(id);
                return Ok("Booking confirmation sent successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST api/<JobController>/5/send-booking-reminder
        [HttpPost("{id}/send-booking-reminder")]
        public async Task<IActionResult> SendBookingReminder(int id)
        {
            try
            {
                await _emailService.SendBookingReminderAsync(id);
                return Ok("Booking reminder sent successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // DELETE api/<JobController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (id == 0)
            {
                return BadRequest("Id cannot be zero.");
            }

            var deleted = await _jobService.DeleteAsync(id);

            if (deleted)
            {
                return Ok("Job deleted successfully.");
            }
            else
            {
                return StatusCode(500, "Failed to delete job.");
            }
        }
    }
}
