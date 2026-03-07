using JMAPI.Models;

namespace JMAPI.Interfaces
{
    public interface IJobServicesService
    {
        Task<List<JobServices>> GetAllAsync();
        Task<IList<Service>> GetByJobIdAsync(int jobId);
        Task<JobServices> CreateAsync(JobServices item);
        Task<bool> UpdateAsync(JobServices item);
        Task<bool> DeleteAsync(int id);
    }
}
