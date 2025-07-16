using JMAPI.Models;
namespace JMAPI.Interfaces
{
    public interface IExpenseItemService
    {
        Task<List<ExpenseItem>> GetAllAsync();
        Task<ExpenseItem?> GetByIdAsync(int id);
        Task<ExpenseItem> CreateAsync(ExpenseItem job);
        Task<bool> UpdateAsync(ExpenseItem job);
        Task<bool> DeleteAsync(int id);
    }
}
