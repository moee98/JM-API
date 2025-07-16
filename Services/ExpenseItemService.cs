using Microsoft.EntityFrameworkCore;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Interfaces;

namespace JMAPI.Services
{
    public class ExpenseItemService : IExpenseItemService
    {
        private readonly AppDbContext _context;

        public ExpenseItemService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ExpenseItem>> GetAllAsync() =>
            await _context.ExpenseItems.ToListAsync();

        public async Task<ExpenseItem?> GetByIdAsync(int id) =>
            await _context.ExpenseItems.FindAsync(id);

        public async Task<ExpenseItem> CreateAsync(ExpenseItem item)
        {
            _context.ExpenseItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateAsync(ExpenseItem item)
        {
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

    }
}
