using JMAPI.Models;
namespace JMAPI.Interfaces
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync(); 
        Task<Customer?> GetByIdAsync(int id); 
        Task<Customer> CreateAsync(Customer customer); 
        Task<bool> UpdateAsync(Customer customer); 
        Task<bool> DeleteAsync(int id); 
    }
}
