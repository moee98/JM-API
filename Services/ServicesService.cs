using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;

namespace JMAPI.Services
{
    public class ServicesService : IServiceTypeService
    {
        private readonly AppDbContext _context;
        public ServicesService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Service>> GetAllAsync() =>
            await _context.Services.ToListAsync();

        public async Task<Service?> GetByIdAsync(int id) =>
            await _context.Services.FindAsync(id);

        public async Task<Service> CreateAsync(Service item)
        {
            

            _context.Services.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateAsync(Service item)
        {
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.Services.FindAsync(id);
            if (item == null) return false;
            _context.Services.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
