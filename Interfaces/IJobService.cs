using JMAPI.Models;

namespace JMAPI.Interfaces
{
    public interface IJobService 
    {
        Task<List<Job>> GetAllAsync(); 
        Task<Job?> GetByIdAsync(int id); 
        Task<Job> CreateAsync(Job job); 
        Task<bool> UpdateAsync(Job job); 
        Task<bool> DeleteAsync(int id); 
    }
}
