using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Interfaces;

namespace JMAPI.Services
{
    public class JobService : IJobService
    {
        private readonly AppDbContext _context;

        public JobService (AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Job>> GetAllAsync() =>
            await _context.Jobs.ToListAsync();

        public async Task<Job?> GetByIdAsync(int id) =>
            await _context.Jobs.FindAsync(id);

        public async Task<Job> CreateAsync(Job item)
        {
            if(item.CustomerId>0)
            {
                var customer = await _context.Customers.FindAsync(item.CustomerId);
                item.Customer = customer;
            }
            
            if(item.CreatedByUserId>0)
            {
                var user = await _context.Users.FindAsync(item.CreatedByUserId);
                item.CreatedByUser = user;
            }
            
            _context.Jobs.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateAsync(Job item)
        {
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
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
