using JMAPI.Models;
namespace JMAPI.Interfaces
{
    public interface IExpenseItemsService
    {
        Task<List<ExpenseItem>> GetAllAsync(DateTime? from = null, DateTime? to = null);
        Task<ExpenseItem?> GetByIdAsync(int id);
        Task<ExpenseItem> CreateAsync(ExpenseItem job);
        Task<bool> UpdateAsync(ExpenseItem job);
        Task<bool> DeleteAsync(int id);
        Task<IList<ExpenseCategory>> GetExpenseCategories();
        Task<ExpenseCategory> CreateExpenseCategory(string name);
    }
}
