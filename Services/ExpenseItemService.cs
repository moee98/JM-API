using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace JMAPI.Services
{
    [Authorize]
    public class ExpenseItemsService : IExpenseItemsService
    {
        private readonly AppDbContext _context;

        public ExpenseItemsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ExpenseItem>> GetAllAsync() =>
            await _context.ExpenseItems.ToListAsync();

        public async Task<ExpenseItem?> GetByIdAsync(int id) =>
            await _context.ExpenseItems
                .Include(x => x.ExpenseCategory)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<ExpenseItem> CreateAsync(ExpenseItem item)
        {
            _context.ExpenseItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateAsync(ExpenseItem item)
        {
            var exists = await _context.ExpenseItems.AnyAsync(x => x.Id == item.Id);
            if (!exists) return false;

            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.ExpenseItems.FindAsync(id);
            if (item == null) return false;
            _context.ExpenseItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IList<ExpenseCategory>> GetExpenseCategories()
        {
            return await _context.ExpenseCategory.ToListAsync();
        }

    }
}
