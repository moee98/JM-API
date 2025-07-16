
using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;


namespace JMAPI.Services
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _context;

        public RoleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserRole>> GetAllAsync() =>
            await _context.UserRoles.ToListAsync();

        public async Task<UserRole?> GetByIdAsync(int id) =>
            await _context.UserRoles.FindAsync(id);

        public async Task<UserRole> CreateAsync(UserRole item)
        {
            _context.UserRoles.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateAsync(UserRole item)
        {
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.UserRoles.FindAsync(id);
            if (item == null) return false;
            _context.UserRoles.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
