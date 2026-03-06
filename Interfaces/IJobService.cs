using JMAPI.Models;

namespace JMAPI.Interfaces
{
    public interface IJobService 
    {
        Task<List<Job>> GetAllAsync(); 
        Task<Job?> GetByIdAsync(int id); 
        Task<Vehicle?> GetVehicleAsync(int jobId);
        Task<Customer?> GetCustomerAsync(int jobId);
        Task<IList<Service?>> GetServicesAsync(int jobId);
       // Task<User?> GetUserAsync(int jobId);
        Task<Job> CreateAsync(Job job); 
        Task<bool> UpdateAsync(int id, Job job); 
        Task<bool> DeleteAsync(int id);
        Task<Job?> GetByIdWithDetailsAsync(int id);
    }
}
