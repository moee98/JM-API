using JMAPI.Models;

namespace JMAPI.Interfaces
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, string title, string message, string type, int? entityId = null);
        Task<List<Notification>> GetUnreadAsync(string userId);
        Task MarkReadAsync(int id, string userId);
        Task MarkAllReadAsync(string userId);
    }
}
