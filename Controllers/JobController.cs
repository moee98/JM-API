using Azure.Core;
using JMAPI.Interfaces;
using JMAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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
                var job = await _jobService.GetByIdAsync(id);
                if (job == null)
                    return NotFound();

                // Fetch related entities in parallel
                var vehicleTask = _jobService.GetVehicleAsync(id);
                var customerTask = _jobService.GetCustomerAsync(id);
                var userTask = _jobService.GetUserAsync(id);
                var servicesTask = _jobService.GetServicesAsync(id);

                await Task.WhenAll(vehicleTask, customerTask, userTask, servicesTask);

                // Assign only if results are not null
                if (vehicleTask.Result != null) job.Vehicle = vehicleTask.Result;
                if (customerTask.Result != null) job.Customer = customerTask.Result;
                if (userTask.Result != null) job.CreatedByUser = userTask.Result;
                if (servicesTask.Result != null) job.Services = servicesTask.Result;

                return Ok(job);
            }
            catch (Exception ex)
            {
                // Log the exception here before returning
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
