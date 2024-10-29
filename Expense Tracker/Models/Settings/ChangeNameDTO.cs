using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.Models.Settings
{
    public class ChangeNameDTO
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
    }
}
