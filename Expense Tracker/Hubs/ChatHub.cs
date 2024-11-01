using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Models;
using Expense_Tracker.Models.Chat;
using System.Collections.Concurrent;

namespace Expense_Tracker.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                UserConnections[userId] = Context.ConnectionId;
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                UserConnections.TryRemove(userId, out _);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessageToUser(string recipientUserId, string message)
        {
            var senderUserId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var senderFullName = $"{Context.User.FindFirst("FirstName")?.Value} {Context.User.FindFirst("LastName")?.Value}";

            if (!await AreFriends(int.Parse(senderUserId), int.Parse(recipientUserId)))
            {
                await Clients.Caller.SendAsync("ErrorMessage", "You are not friends with this user.");
                return;
            }

            var messageEntity = new Message
            {
                SenderId = int.Parse(senderUserId),
                ReceiverId = int.Parse(recipientUserId),
                Content = message,
                Timestamp = DateTime.Now
            };

            _context.Messages.Add(messageEntity);
            await _context.SaveChangesAsync();

            if (UserConnections.TryGetValue(recipientUserId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderUserId, senderFullName, message);
            }
            await Clients.Caller.SendAsync("ReceiveMessage", senderUserId, senderFullName, message);
        }

        public async Task LoadChatHistory(string friendId)
        {
            var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == int.Parse(friendId)) ||
                            (m.SenderId == int.Parse(friendId) && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.SenderId,
                    m.ReceiverId,
                    SenderFullName = m.SenderId == userId ? "You" : $"{m.Sender.FirstName} {m.Sender.LastName}",
                    m.Content
                })
                .ToListAsync();

            foreach (var message in messages)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", message.SenderId, message.SenderFullName, message.Content);
            }
        }


        private async Task<bool> AreFriends(int userId1, int userId2)
        {
            if (userId1 > userId2)
            {
                (userId1, userId2) = (userId2, userId1);
            }

            return await _context.UserFriends.AnyAsync(f => f.UserId1 == userId1 && f.UserId2 == userId2);
        }
    }
}
