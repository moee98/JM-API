using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;

namespace JMAPI.Services
{
    public class VehicleInspectionService : IVehicleInspectionService
    {
        private readonly AppDbContext _context;
        public VehicleInspectionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleInspection>> GetAllAsync() =>
            await _context.VehicleInspection.ToListAsync();

        public async Task<VehicleInspection?> GetByIdAsync(int id) =>
            await _context.VehicleInspection.FindAsync(id);

        public async Task<VehicleInspection> CreateAsync(VehicleInspection item)
        {


            _context.VehicleInspection.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateAsync(VehicleInspection item)
        {
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.VehicleInspection.FindAsync(id);
            if (item == null) return false;
            _context.VehicleInspection.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
