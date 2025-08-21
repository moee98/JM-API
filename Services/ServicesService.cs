using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;

namespace JMAPI.Services
{
    public class ServicesService : IServiceTypeService
    {
        private readonly AppDbContext _context;
        private readonly ServicesService _services;
        public ServicesService(AppDbContext context)
        {
            
            _context = context;
            _services = new ServicesService(context);
        }

        public async Task<List<Service>> GetAllAsync() =>
            await _services.GetAllAsync();

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
