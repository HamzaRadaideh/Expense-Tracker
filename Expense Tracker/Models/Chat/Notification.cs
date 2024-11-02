using Expense_Tracker.Models.Account;
using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.Models.Chat
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string Message { get; set; }

        public string NotificationType { get; set; } // E.g., "Chat", "FriendRequest"

        public bool IsRead { get; set; } = false;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
