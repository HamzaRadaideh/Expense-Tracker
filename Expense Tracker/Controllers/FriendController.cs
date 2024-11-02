using Expense_Tracker.Models.Chat;
using Expense_Tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class FriendController(ApplicationDbContext context) : Controller
    {

        // GET: AddFriend - Renders a view with a search bar for finding users
        [HttpGet]
        public IActionResult AddFriend()
        {
            return View();
        }

        // GET: Search - Searches for users by first or last name
        [HttpGet]
        public async Task<IActionResult> Search(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return BadRequest("Search query cannot be empty.");
            }

            // Get the current user's ID
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Perform a search on the Users table, excluding the current user
            var users = await context.Users
                .Where(u => (u.FirstName.Contains(searchQuery) || u.LastName.Contains(searchQuery)) && u.Id != currentUserId)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName
                })
                .ToListAsync();

            return PartialView("_UserSearchResults", users);
        }


        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(int receiverId)
        {
            var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Check if a request is already sent
            if (context.FriendRequests.Any(r => r.RequesterId == requesterId && r.ReceiverId == receiverId && r.Status == "Pending"))
            {
                Json(new { success = false, message = "Friend request already sent." });

                return RedirectToAction("AddFriend", "Friend");
            }

            var request = new FriendRequest
            {
                RequesterId = requesterId,
                ReceiverId = receiverId,
                Status = "Pending",
                RequestDate = DateTime.Now
            };

            context.FriendRequests.Add(request);
            await context.SaveChangesAsync();

            Json(new { success = true, message = "Friend request sent successfully." });

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Friend request sent successfully.";
            return RedirectToAction("AddFriend", "Friend");
        }


        // GET: GetPendingRequests - Renders a view showing pending friend requests
        [HttpGet]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var pendingRequests = await context.FriendRequests
                .Where(r => r.ReceiverId == userId && r.Status == "Pending")
                .Include(r => r.Requester) // Include the requester details
                .ToListAsync();

            return View(pendingRequests);
        }

        // POST: RespondToFriendRequest - Accepts or denies a friend request
        [HttpPost]
        public async Task<IActionResult> RespondToFriendRequest(int requestId, string response)
        {
            var request = await context.FriendRequests.FindAsync(requestId);
            if (request == null || request.Status != "Pending")
            {
                return BadRequest("Request not found or already responded to.");
            }

            if (response == "Accept")
            {
                request.Status = "Accepted";

                var friendship = new UserFriend
                {
                    UserId1 = Math.Min(request.RequesterId, request.ReceiverId),
                    UserId2 = Math.Max(request.RequesterId, request.ReceiverId)
                };

                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Friend request Accepted successfully.";

                context.UserFriends.Add(friendship);
            }
            else if (response == "Deny")
            {
                request.Status = "Denied";

                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Friend request Denied successfully.";
            }
            else
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Invalid response.";
                return BadRequest("Invalid response.");
            }

            await context.SaveChangesAsync();
            return Ok("Friend request updated.");
        }

        // POST: RemoveFriend - Removes a friend from the user's friend list
        [HttpPost]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var friendship = await context.UserFriends
                .FirstOrDefaultAsync(f => (f.UserId1 == userId && f.UserId2 == friendId) || (f.UserId1 == friendId && f.UserId2 == userId));

            if (friendship == null)
            {
                TempData["ToastType"] = "Error";
                TempData["ToastMessage"] = "Friendship not found.";
                return NotFound("Friendship not found.");
            }

            context.UserFriends.Remove(friendship);
            await context.SaveChangesAsync();

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Friend removed. successfully.";

            return Ok("Friend removed.");
        }

        // GET: Chats - Lists friends for selecting a chat
        [HttpGet]
        public async Task<IActionResult> Chats()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var friends = await context.UserFriends
                .Where(f => f.UserId1 == userId || f.UserId2 == userId)
                .Select(f => f.UserId1 == userId ? f.User2 : f.User1)
                .ToListAsync();

            return View(friends);
        }
    }
}