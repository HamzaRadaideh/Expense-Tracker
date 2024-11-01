using Expense_Tracker.Models.Account;

namespace Expense_Tracker.Models.Chat
{
    public class UserFriend
    {
        public int Id { get; set; }

        // Always the smaller user ID in the friendship
        public int UserId1 { get; set; }
        public User User1 { get; set; }

        // Always the larger user ID in the friendship
        public int UserId2 { get; set; }
        public User User2 { get; set; }
    }
}
