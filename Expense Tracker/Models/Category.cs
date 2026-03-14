using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Expense_Tracker.Models
{
    public class Category
    {
        private static readonly Regex FontAwesomeClassPattern = new(
            @"^(fa(?:s|r|l|b|d)?\s+)?fa-[a-z0-9-]+(?:\s+fa-[a-z0-9-]+)*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        [Key]
        public int CategoryId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(50)")]
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        [Column(TypeName = "nvarchar(100)")]
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
        public string IconCssClass
        {
            get
            {
                var iconValue = (Icon ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(iconValue))
                {
                    return "fa-solid fa-tag";
                }

                if (!FontAwesomeClassPattern.IsMatch(iconValue))
                {
                    return "fa-solid fa-tag";
                }

                if (iconValue.StartsWith("fa-", StringComparison.OrdinalIgnoreCase))
                {
                    return $"fa-solid {iconValue}";
                }

                return iconValue;
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
