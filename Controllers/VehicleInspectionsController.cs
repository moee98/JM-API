using JMAPI.Database;
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
    public class VehicleInspectionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleInspectionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/VehicleInspections
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleInspection>>> GetVehicleInspection()
        {
            return await _context.VehicleInspection.ToListAsync();
        }

        // GET: api/VehicleInspections/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleInspection>> GetVehicleInspection(int id)
        {
            var vehicleInspection = await _context.VehicleInspection.FindAsync(id);

            if (vehicleInspection == null)
            {
                return NotFound();
            }

            return vehicleInspection;
        }

        // PUT: api/VehicleInspections/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicleInspection(int id, VehicleInspection vehicleInspection)
        {
            if (id != vehicleInspection.Id)
            {
                return BadRequest();
            }

            _context.Entry(vehicleInspection).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleInspectionExists(id))
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

        // POST: api/VehicleInspections
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<VehicleInspection>> PostVehicleInspection(VehicleInspection vehicleInspection)
        {
            _context.VehicleInspection.Add(vehicleInspection);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVehicleInspection", new { id = vehicleInspection.Id }, vehicleInspection);
        }

        // DELETE: api/VehicleInspections/5
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

        private bool VehicleInspectionExists(int id)
        {
            return _context.VehicleInspection.Any(e => e.Id == id);
        }
    }
}
