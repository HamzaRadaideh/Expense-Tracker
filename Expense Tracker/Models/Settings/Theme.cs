using Expense_Tracker.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense_Tracker.Models.Settings
{
    public class Theme
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public bool IsDarkTheme { get; set; }  // true for dark theme, false for light theme

        // Foreign key to User
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
