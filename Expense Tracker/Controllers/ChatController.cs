using Expense_Tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(int friendId)
        {
            // Pass the friend's user ID to the view
            ViewBag.FriendId = friendId;
            return View();
        }

        public async Task<IActionResult> Notificain()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.Timestamp)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null || notification.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value))
                return BadRequest();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}