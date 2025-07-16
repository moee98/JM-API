using JMAPI.Models;
using JMAPI.Services;
namespace JMAPI.Interfaces
{
    public interface IServiceTypeService
    {
        Task<List<Service>> GetAllAsync();
        Task<Service> GetByIdAsync(int id);
        Task<Service> CreateAsync(Service item);
        Task<bool> UpdateAsync(Service item);
        Task<bool> DeleteAsync(int id);
    }
}
