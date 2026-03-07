using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;

namespace JMAPI.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly AppDbContext _context;
        public VehicleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Vehicle>> GetAllAsync() =>
            await _context.Vehicles.ToListAsync();

        public async Task<Vehicle?> GetByIdAsync(int id) =>
            await _context.Vehicles.FindAsync(id);

        public async Task<Vehicle?> GetByJobIdAsync(int jobId) =>
            await _context.Jobs
                .Where(x => x.Id == jobId)
                .Select(js => js.Vehicle)
                .FirstOrDefaultAsync();

        public async Task<Vehicle> CreateAsync(Vehicle item)
        {
            _context.Vehicles.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateAsync(Vehicle item)
        {
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.Vehicles.FindAsync(id);
            if (item == null) return false;
            _context.Vehicles.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
