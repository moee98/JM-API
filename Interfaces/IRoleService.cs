using JMAPI.Models;
namespace JMAPI.Interfaces
{
    public interface IRoleService
    {
        Task<List<UserRole>> GetAllAsync();
        Task<UserRole?> GetByIdAsync(int id);
        Task<UserRole> CreateAsync(UserRole job);
        Task<bool> UpdateAsync(UserRole job);
        Task<bool> DeleteAsync(int id);
    }
}
