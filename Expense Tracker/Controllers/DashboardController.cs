using Expense_Tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            //Last 7 Days
            DateTime StartDate = DateTime.Today.AddDays(-9);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            //Total Income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category?.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            //Total Expense
            int TotalExpense = SelectedTransactions
                .Where(i => i.Category?.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            //Balance
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-IN");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category?.Type == "Expense")
                .GroupBy(j => j.CategoryId)
                .Select(k => new
                {
                    categoryTitle = (k.First().Category != null)
                        ? k.First().Category!.Title
                        : "Unknown Category",
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            //Spline Chart - Income vs Expense

            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category?.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category?.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            //Combine Income & Expense
            string[] Last10Days = Enumerable.Range(0, 10)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last10Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            // Budget vs Expense (current month)
            DateTime currentMonthStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime currentMonthEndDate = currentMonthStartDate.AddMonths(1).AddDays(-1);

            var monthlyExpenseByCategory = await _context.Transactions
                .Include(i => i.Category)
                .Where(i => i.Date >= currentMonthStartDate && i.Date <= currentMonthEndDate)
                .Where(i => i.Category != null && i.Category.Type == "Expense")
                .GroupBy(i => i.CategoryId)
                .Select(i => new
                {
                    categoryId = i.Key,
                    spent = i.Sum(j => j.Amount)
                })
                .ToDictionaryAsync(i => i.categoryId, i => i.spent);

            var expenseBudgetCategories = await _context.Categories
                .Where(i => i.Type == "Expense" && i.MonthlyBudget.HasValue && i.MonthlyBudget.Value > 0)
                .OrderBy(i => i.Title)
                .ToListAsync();

            var budgetStatus = expenseBudgetCategories.Select(i =>
            {
                int spentAmount = monthlyExpenseByCategory.TryGetValue(i.CategoryId, out var amount) ? amount : 0;
                int budgetAmount = i.MonthlyBudget ?? 0;
                int remainingAmount = budgetAmount - spentAmount;
                decimal usagePercentage = budgetAmount == 0 ? 0 : (decimal)spentAmount / budgetAmount * 100;

                return new
                {
                    categoryTitle = i.Title,
                    categoryIconCssClass = i.IconCssClass,
                    budgetValue = budgetAmount,
                    spentValue = spentAmount,
                    budget = budgetAmount.ToString("C0"),
                    spent = spentAmount.ToString("C0"),
                    remaining = remainingAmount.ToString("C0"),
                    usagePercentage = Math.Round(usagePercentage, 1),
                    isOverBudget = spentAmount > budgetAmount
                };
            }).ToList();

            ViewBag.TotalMonthlyBudget = expenseBudgetCategories.Sum(i => i.MonthlyBudget ?? 0).ToString("C0");
            ViewBag.TotalMonthlySpent = budgetStatus.Sum(i => i.spentValue).ToString("C0");
            ViewBag.BudgetStatus = budgetStatus;

            // Accounts summary and credit dues
            List<WalletAccount> walletAccounts = await _context.WalletAccounts
                .OrderBy(i => i.Name)
                .ToListAsync();
            List<AccountTransfer> accountTransfers = await _context.AccountTransfers.ToListAsync();
            List<Transaction> allTransactionsWithAccounts = await _context.Transactions
                .Include(i => i.Category)
                .Where(i => i.WalletAccountId.HasValue)
                .ToListAsync();

            var accountBalanceSummary = walletAccounts.Select(account =>
            {
                int income = allTransactionsWithAccounts
                    .Where(i => i.WalletAccountId == account.WalletAccountId && i.Category?.Type == "Income")
                    .Sum(i => i.Amount);
                int expense = allTransactionsWithAccounts
                    .Where(i => i.WalletAccountId == account.WalletAccountId && i.Category?.Type == "Expense")
                    .Sum(i => i.Amount);
                int incomingTransfers = accountTransfers
                    .Where(i => i.ToWalletAccountId == account.WalletAccountId)
                    .Sum(i => i.Amount);
                int outgoingTransfers = accountTransfers
                    .Where(i => i.FromWalletAccountId == account.WalletAccountId)
                    .Sum(i => i.Amount);

                int currentBalance = account.OpeningBalance + income - expense + incomingTransfers - outgoingTransfers;
                int creditDue = account.IsCreditCard && currentBalance < 0 ? Math.Abs(currentBalance) : 0;

                return new
                {
                    accountName = account.Name,
                    accountType = account.Type,
                    balance = currentBalance.ToString("C0"),
                    balanceValue = currentBalance,
                    creditDue = creditDue == 0 ? "-" : creditDue.ToString("C0"),
                    creditDueValue = creditDue
                };
            }).ToList();

            ViewBag.AccountBalanceSummary = accountBalanceSummary;
            ViewBag.TotalWalletBalance = accountBalanceSummary.Sum(i => i.balanceValue).ToString("C0");
            ViewBag.TotalCreditCardDue = accountBalanceSummary.Sum(i => i.creditDueValue).ToString("C0");

            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .Include(i => i.WalletAccount)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();


            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;

    }
}
