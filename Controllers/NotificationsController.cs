using JMAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JMAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notifications = await _notificationService.GetUnreadAsync(userId);
            return Ok(notifications);
        }

        // PUT: api/notifications/5/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkReadAsync(id, userId);
            return NoContent();
        }

        // PUT: api/notifications/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAllReadAsync(userId);
            return NoContent();
        }
    }
}
