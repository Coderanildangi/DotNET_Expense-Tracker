using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense_Tracker.Models
{
    public class AccountTransfer
    {
        [Key]
        public int AccountTransferId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select source account.")]
        public int FromWalletAccountId { get; set; }
        public WalletAccount? FromWalletAccount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select destination account.")]
        public int ToWalletAccountId { get; set; }
        public WalletAccount? ToWalletAccount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Amount should be greater than 0.")]
        public int Amount { get; set; }

        [Column(TypeName = "nvarchar(120)")]
        public string? Note { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAtUtc { get; set; }

        [NotMapped]
        public string FormattedAmount => Amount.ToString("C0");
    }
}
