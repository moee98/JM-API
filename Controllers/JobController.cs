using Azure.Core;
using JMAPI.Interfaces;
using JMAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace JMAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobService;

        public JobController(IJobService jobService)
        {
            _jobService = jobService;
        }

        // GET: api/<JobController>
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var jobs = await _jobService.GetAllAsync();
            if (jobs !=null)
            {
                return Ok(jobs);
            }
            else 
            {
                return NotFound("No jobs found.");
            }

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
        public async Task<IActionResult> Post([FromBody] Job job  )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (job == null)
            {
                return BadRequest("Job cannot be null.");
            }
            else
            {
                try
                {
                    var created = await _jobService.CreateAsync(job);
                    if(created == null)
                    {
                        return StatusCode(500, "Failed to create job.");
                    }
                    
                    return StatusCode(200, created);
                }
                catch(Exception ex)
                {
                    return StatusCode(500,ex.Message + ex.InnerException);
                }
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
