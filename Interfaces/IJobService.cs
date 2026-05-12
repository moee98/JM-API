using JMAPI.Models;

namespace JMAPI.Interfaces
{
    public interface IJobService
    {
        Task<PaginatedResult<Job>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null, bool? paid = null);
        Task<List<Job>> GetOutstandingAsync();
        Task<Job?> GetByIdAsync(int id);
        Task<Vehicle?> GetVehicleAsync(int jobId);
        Task<Customer?> GetCustomerAsync(int jobId);
        Task<IList<Service?>> GetServicesAsync(int jobId);
        Task<Job> CreateAsync(Job job);
        Task<bool> UpdateAsync(int id, Job job);
        Task<bool> DeleteAsync(int id);
        Task<Job?> GetByIdWithDetailsAsync(int id);
    }
}
