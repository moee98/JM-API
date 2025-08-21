using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;

namespace JMAPI.Services
{
    public class JobServicesService : IJobServicesService
    {
        private readonly AppDbContext _context;

        public JobServicesService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<JobServices>> GetAllAsync() =>
            await _context.JobServices.ToListAsync();

        public async Task<IList<Service?>> GetByJobIdAsync(int jobId) =>
            await _context.JobServices.Where(x => x.JobId == jobId).Include(js => js.Service).Select(js => js.Service).ToListAsync();
            
        
           

        public async Task<JobServices> CreateAsync(JobServices item)
        {
            _context.JobServices.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateAsync(JobServices item)
        {
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.JobServices.FindAsync(id);
            if (item == null) return false;
            _context.JobServices.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
