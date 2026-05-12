using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JMAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(string userId, string title, string message, string type, int? entityId = null)
        {
            _context.Notifications.Add(new Notification
            {
                AppUserId = userId,
                Title = title,
                Message = message,
                Type = type,
                EntityId = entityId
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUnreadAsync(string userId) =>
            await _context.Notifications
                .Where(n => n.AppUserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task MarkReadAsync(int id, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.AppUserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllReadAsync(string userId)
        {
            await _context.Notifications
                .Where(n => n.AppUserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }
    }
}
