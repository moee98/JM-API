using JMAPI.Models;
namespace JMAPI.Interfaces
{
    public interface IVehicleInspectionService
    {
        Task<List<VehicleInspection>> GetAllAsync();
        Task<VehicleInspection?> GetByIdAsync(int id);
        Task<VehicleInspection> CreateAsync(VehicleInspection item);
        Task<bool> UpdateAsync(VehicleInspection item);
        Task<bool> DeleteAsync(int id);
    }
}
