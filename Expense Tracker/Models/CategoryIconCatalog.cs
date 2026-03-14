using System.Collections.Generic;
using System.Linq;

namespace Expense_Tracker.Models
{
    public class CategoryIconOption
    {
        public string CssClass { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public static class CategoryIconCatalog
    {
        public static readonly IReadOnlyList<CategoryIconOption> Options = new List<CategoryIconOption>
        {
            new() { CssClass = "fa-solid fa-wallet", DisplayName = "Wallet" },
            new() { CssClass = "fa-solid fa-money-bill-wave", DisplayName = "Cash Flow" },
            new() { CssClass = "fa-solid fa-building-columns", DisplayName = "Bank" },
            new() { CssClass = "fa-solid fa-briefcase", DisplayName = "Work" },
            new() { CssClass = "fa-solid fa-chart-line", DisplayName = "Investments" },
            new() { CssClass = "fa-solid fa-hand-holding-dollar", DisplayName = "Reimbursement" },
            new() { CssClass = "fa-solid fa-house", DisplayName = "Home" },
            new() { CssClass = "fa-solid fa-cart-shopping", DisplayName = "Groceries" },
            new() { CssClass = "fa-solid fa-basket-shopping", DisplayName = "Shopping" },
            new() { CssClass = "fa-solid fa-utensils", DisplayName = "Food" },
            new() { CssClass = "fa-solid fa-mug-saucer", DisplayName = "Cafe" },
            new() { CssClass = "fa-solid fa-car", DisplayName = "Car" },
            new() { CssClass = "fa-solid fa-gas-pump", DisplayName = "Fuel" },
            new() { CssClass = "fa-solid fa-plane", DisplayName = "Travel" },
            new() { CssClass = "fa-solid fa-bus", DisplayName = "Bus" },
            new() { CssClass = "fa-solid fa-train", DisplayName = "Train" },
            new() { CssClass = "fa-solid fa-bolt", DisplayName = "Electricity" },
            new() { CssClass = "fa-solid fa-droplet", DisplayName = "Water" },
            new() { CssClass = "fa-solid fa-wifi", DisplayName = "Internet" },
            new() { CssClass = "fa-solid fa-phone", DisplayName = "Phone" },
            new() { CssClass = "fa-solid fa-film", DisplayName = "Entertainment" },
            new() { CssClass = "fa-solid fa-gamepad", DisplayName = "Gaming" },
            new() { CssClass = "fa-solid fa-heart-pulse", DisplayName = "Health" },
            new() { CssClass = "fa-solid fa-graduation-cap", DisplayName = "Education" },
            new() { CssClass = "fa-solid fa-gift", DisplayName = "Gifts" }
        };

        public static bool IsValidIcon(string iconCssClass)
        {
            return Options.Any(i => i.CssClass.Equals(iconCssClass, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
