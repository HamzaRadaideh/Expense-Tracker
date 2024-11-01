using Expense_Tracker.Models.Account;

namespace Expense_Tracker.Models.Chat
{
    public class FriendRequest
    {
        public int FriendRequestId { get; set; }
        public int RequesterId { get; set; }
        public User Requester { get; set; }
        public int ReceiverId { get; set; }
        public User Receiver { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RequestDate { get; set; } = DateTime.Now;
    }
}
