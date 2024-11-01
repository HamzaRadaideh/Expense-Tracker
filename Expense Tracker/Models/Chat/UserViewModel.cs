namespace Expense_Tracker.Models.Chat
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Combine first and last name for display purposes
        public string FullName => $"{FirstName} {LastName}";
    }
}