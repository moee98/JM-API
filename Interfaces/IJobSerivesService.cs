using JMAPI.Models;

namespace JMAPI.Interfaces
{
    public interface IJobServicesService
    {
        Task<List<JobServices>> GetAllAsync();
        Task<JobServices?> GetByIdAsync(int id);
        Task<JobServices> CreateAsync(JobServices item);
        Task<bool> UpdateAsync(JobServices item);
        Task<bool> DeleteAsync(int id);
    }
}
