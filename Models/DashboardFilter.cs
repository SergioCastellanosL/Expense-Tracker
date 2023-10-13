using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.Models
{
    public class DashboardFilter
    {
        public int AccountId { get; set; }
        public Account? Account { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public string MonthOrYear { get; set; }
    }
}
