using JMAPI.Models;
namespace JMAPI.Interfaces
{
    public interface IVehicleService
    {
        Task<List<Vehicle>> GetAllAsync();
        Task<Vehicle> GetByIdAsync(int id);
        Task<Vehicle> CreateAsync(Vehicle item);
        Task<bool> UpdateAsync(Vehicle item);
        Task<bool> DeleteAsync(int id);
    }
}
