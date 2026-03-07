using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace JMAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class JobServicesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJobServicesService _jobServices;

        public JobServicesController(AppDbContext context, IJobServicesService jobServices)
        {
            _context = context;
            _jobServices = jobServices;
        }

        // GET: api/JobServices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobServices>>> GetJobServices()
        {
            return await _context.JobServices.ToListAsync();
        }

        // GET: api/JobServices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JobServices>> GetJobServices(int id)
        {
            var jobServices = await _context.JobServices.FindAsync(id);

            if (jobServices == null)
            {
                return NotFound();
            }

            return jobServices;
        }

        [HttpGet("job/{jobId:int}")]
        public async Task<ActionResult<IList<Service>>> GetServicesByJob(int jobId)
        {
            var jobServices = await _jobServices.GetByJobIdAsync(jobId);
            return Ok(jobServices);
        }

        // PUT: api/JobServices/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutJobServices(int id, JobServices jobServices)
        {
            if (id != jobServices.id)
            {
                return BadRequest();
            }

            _context.Entry(jobServices).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobServicesExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/JobServices
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<JobServices>> PostJobServices(JobServices jobServices)
        {
            _context.JobServices.Add(jobServices);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetJobServices", new { id = jobServices.id }, jobServices);
        }

        // DELETE: api/JobServices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJobServices(int id)
        {
            var jobServices = await _context.JobServices.FindAsync(id);
            if (jobServices == null)
            {
                return NotFound();
            }

            _context.JobServices.Remove(jobServices);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool JobServicesExists(int id)
        {
            return _context.JobServices.Any(e => e.id == id);
        }
    }
}
