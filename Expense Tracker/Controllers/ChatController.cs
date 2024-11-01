using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        [HttpGet]
        public IActionResult Index(int friendId)
        {
            // Pass the friend's user ID to the view
            ViewBag.FriendId = friendId;
            return View();
        }
    }
}
