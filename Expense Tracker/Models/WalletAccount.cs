using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense_Tracker.Models
{
    public class WalletAccount
    {
        [Key]
        public int WalletAccountId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account name is required.")]
        [Column(TypeName = "nvarchar(80)")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        public string Type { get; set; } = "Cash";

        [Range(0, int.MaxValue, ErrorMessage = "Opening balance cannot be negative.")]
        public int OpeningBalance { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Credit limit cannot be negative.")]
        public int? CreditLimit { get; set; }

        public DateTime? DueDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAtUtc { get; set; }

        [NotMapped]
        public bool IsCreditCard => Type.Equals("Credit Card", StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public string OpeningBalanceFormatted => OpeningBalance.ToString("C0");

        [NotMapped]
        public string CreditLimitFormatted => CreditLimit.HasValue ? CreditLimit.Value.ToString("C0") : "-";
    }
}
