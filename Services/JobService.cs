using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JMAPI.Services
{
    [Authorize]
    public class JobService : IJobService
    {
        private readonly AppDbContext _context;

        public JobService (AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Job>> GetAllAsync() =>
            await _context.Jobs.Include(js => js.Vehicle).ToListAsync();

        public async Task<Job?> GetByIdAsync(int id) =>
            await _context.Jobs.Where(x=> x.Id == id).Include(js => js.Vehicle).FirstOrDefaultAsync();
        
        public async Task<Vehicle?> GetVehicleAsync(int jobId) =>
           await _context.Jobs.Where(x => x.Id == jobId).Include(js => js.Vehicle).Select(js => js.Vehicle).FirstOrDefaultAsync();

        public async Task<Customer?> GetCustomerAsync(int jobId) =>
           await _context.Jobs.Where(x => x.Id == jobId).Include(js => js.Customer).Select(js => js.Customer).FirstOrDefaultAsync();

        public async Task<AppUser?> GetUserAsync(int jobId) =>
          await _context.Jobs.Where(x => x.Id == jobId).Include(js => js.AppUser).Select(js => js.AppUser).FirstOrDefaultAsync();
        public async Task<IList<Service?>> GetServicesAsync(int jobId) =>
            await _context.JobServices.Where(x => x.JobId == jobId).Include(js => js.Service).Select(js => js.Service).ToListAsync();
        public async Task<Job> CreateAsync(Job item)
        {
            if(item.CustomerId>0)
            {
                var customer = await _context.Customers.FindAsync(item.CustomerId);
                item.Customer = customer;
            }
            

            
            _context.Jobs.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
        public async Task<Job?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Jobs
                .Include(j => j.Vehicle)
                .Include(j => j.Customer)
                .Include(j => j.JobServices).ThenInclude(js => js.Service)       // or .Include(j => j.JobServices).ThenInclude(js => js.Service)
                .FirstOrDefaultAsync(j => j.Id == id);
        }


        // Service
        public async Task<bool> UpdateAsync(int id, Job item)
        {
            var existing = await _context.Jobs
                .Include(j => j.Vehicle)
                .Include(j => j.JobServices)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (existing == null) return false;

            // Update root fields
            existing.Description = item.Description;
            existing.Status = item.Status;
            existing.DueDate = item.DueDate;
            existing.Notes = item.Notes;
            existing.ServiceCharge = item.ServiceCharge;
            existing.Paid = item.Paid;
            existing.PaymentMethod = item.PaymentMethod;
            existing.AppUserId = item.AppUserId;
            existing.IsActive = item.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            // Update vehicle
            if (item.Vehicle != null)
            {
                if (existing.Vehicle == null)
                {
                    existing.Vehicle = new Vehicle
                    {
                        LicensePlate = item.Vehicle.LicensePlate,
                        Make = item.Vehicle.Make,
                        Model = item.Vehicle.Model,
                        Colour = item.Vehicle.Colour,
                        
                        IsActive = item.Vehicle.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    existing.Vehicle.LicensePlate = item.Vehicle.LicensePlate;
                    existing.Vehicle.Make = item.Vehicle.Make;
                    existing.Vehicle.Model = item.Vehicle.Model;
                    existing.Vehicle.Colour = item.Vehicle.Colour;
                    
                    existing.Vehicle.IsActive = item.Vehicle.IsActive;
                    existing.Vehicle.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Sync JobServices
            var incoming = item.JobServices;

            var incomingIds = incoming.Where(x => x.id > 0).Select(x => x.id).ToHashSet();

            var toRemove = existing.JobServices
                .Where(db => db.id > 0 && !incomingIds.Contains(db.id))
                .ToList();

            if (toRemove.Count > 0)
                _context.JobServices.RemoveRange(toRemove);

            foreach (var inc in incoming)
            {
                JobServices? dbRow = null;

                if (inc.id > 0)
                    dbRow = existing.JobServices.FirstOrDefault(x => x.id == inc.id);

                if (dbRow == null)
                {
                    existing.JobServices.Add(new JobServices
                    {
                        ServiceId = inc.ServiceId,
                        Price = inc.Price,
                        Completed = inc.Completed
                    });
                }
                else
                {
                    dbRow.ServiceId = inc.ServiceId;
                    dbRow.Price = inc.Price;
                    dbRow.Completed = inc.Completed;
                }
            }

            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        } 

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.Jobs.FindAsync(id);
            if (item == null) return false;
            _context.Jobs.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
