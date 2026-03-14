using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense_Tracker.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(50)")]
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        [Column(TypeName = "nvarchar(5)")]
        public string Icon { get; set; } = "";

        [Column(TypeName = "nvarchar(10)")]
        public string Type { get; set; } = "Expense";

        [Column(TypeName = "int")]
        [Range(0, int.MaxValue, ErrorMessage = "Monthly budget cannot be negative.")]
        public int? MonthlyBudget { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAtUtc { get; set; }

        [NotMapped]
        public string? TitleWithIcon
        {
            get
            {
                return this.Icon + " " + this.Title;
            }
        }

        [NotMapped]
        public string MonthlyBudgetFormatted
        {
            get
            {
                return MonthlyBudget.HasValue ? MonthlyBudget.Value.ToString("C0") : "-";
            }
        }
    }
}
