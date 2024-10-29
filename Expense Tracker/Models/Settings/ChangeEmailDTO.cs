using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.Models.Settings
{
    public class ChangeEmailDTO
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; }

        [Required]
        [Compare("NewEmail", ErrorMessage = "Emails do not match.")]
        public string ConfirmEmail { get; set; }
    }
}
