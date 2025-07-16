using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JMAPI.Interfaces;
using JMAPI.Models;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JMAPI.Controllers
{
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
        public IActionResult Get()
        {
            var jobs = _jobService.GetAllAsync();
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
        public string Get(int id)
        {
            var jobs = _jobService.GetByIdAsync(id);
            if (jobs != null)
            {
                return jobs.ToString();
            }
            else
            {
                return "No jobs found.";
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
                    //return CreatedAtAction(await _jobService(GetJobById(), new { id = created.Id }, created);
                    // return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
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
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<JobController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
