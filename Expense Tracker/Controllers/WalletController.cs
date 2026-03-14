using Expense_Tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WalletController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        public async Task<IActionResult> Index()
        {
            List<WalletAccount> accounts = await _context.WalletAccounts
                .OrderBy(i => i.Name)
                .ToListAsync();

            List<AccountTransfer> transfers = await _context.AccountTransfers.ToListAsync();
            List<Transaction> transactions = await _context.Transactions
                .Include(i => i.Category)
                .Where(i => i.WalletAccountId.HasValue)
                .ToListAsync();

            var accountSummary = accounts.Select(account =>
            {
                int income = transactions
                    .Where(i => i.WalletAccountId == account.WalletAccountId && i.Category?.Type == "Income")
                    .Sum(i => i.Amount);
                int expense = transactions
                    .Where(i => i.WalletAccountId == account.WalletAccountId && i.Category?.Type == "Expense")
                    .Sum(i => i.Amount);
                int incomingTransfers = transfers
                    .Where(i => i.ToWalletAccountId == account.WalletAccountId)
                    .Sum(i => i.Amount);
                int outgoingTransfers = transfers
                    .Where(i => i.FromWalletAccountId == account.WalletAccountId)
                    .Sum(i => i.Amount);

                int currentBalance = account.OpeningBalance + income - expense + incomingTransfers - outgoingTransfers;
                int creditDue = account.IsCreditCard && currentBalance < 0 ? Math.Abs(currentBalance) : 0;

                return new
                {
                    walletAccountId = account.WalletAccountId,
                    name = account.Name,
                    type = account.Type,
                    openingBalance = account.OpeningBalance.ToString("C0"),
                    currentBalanceValue = currentBalance,
                    currentBalance = currentBalance.ToString("C0"),
                    creditLimit = account.CreditLimit.HasValue ? account.CreditLimit.Value.ToString("C0") : "-",
                    dueDate = account.DueDate.HasValue ? account.DueDate.Value.ToString("dd-MMM-yyyy") : "-",
                    creditDueValue = creditDue,
                    creditDue = creditDue == 0 ? "-" : creditDue.ToString("C0")
                };
            }).ToList();

            ViewBag.AccountSummary = accountSummary;
            ViewBag.TotalBalance = accountSummary.Sum(i => i.currentBalanceValue).ToString("C0");
            ViewBag.TotalCreditDue = accountSummary.Sum(i => i.creditDueValue).ToString("C0");

            return View(accounts);
        }

        public IActionResult AddOrEdit(int id = 0)
        {
            ViewBag.AccountTypes = GetAccountTypes();
            if (id == 0)
            {
                return View(new WalletAccount());
            }

            WalletAccount? account = _context.WalletAccounts.FirstOrDefault(i => i.WalletAccountId == id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("WalletAccountId,Name,Type,OpeningBalance,CreditLimit,DueDate")] WalletAccount walletAccount)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Challenge();
            }

            if (walletAccount.Type != "Credit Card")
            {
                walletAccount.CreditLimit = null;
                walletAccount.DueDate = null;
            }

            if (ModelState.IsValid)
            {
                if (walletAccount.WalletAccountId == 0)
                {
                    walletAccount.UserId = CurrentUserId;
                    _context.WalletAccounts.Add(walletAccount);
                }
                else
                {
                    WalletAccount? accountToUpdate = await _context.WalletAccounts
                        .FirstOrDefaultAsync(i => i.WalletAccountId == walletAccount.WalletAccountId);
                    if (accountToUpdate == null)
                    {
                        return NotFound();
                    }

                    accountToUpdate.Name = walletAccount.Name;
                    accountToUpdate.Type = walletAccount.Type;
                    accountToUpdate.OpeningBalance = walletAccount.OpeningBalance;
                    accountToUpdate.CreditLimit = walletAccount.CreditLimit;
                    accountToUpdate.DueDate = walletAccount.DueDate;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AccountTypes = GetAccountTypes();
            return View(walletAccount);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            WalletAccount? account = await _context.WalletAccounts.FirstOrDefaultAsync(i => i.WalletAccountId == id);
            if (account == null)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction(nameof(Index));
            }

            bool hasTransactions = await _context.Transactions.AnyAsync(i => i.WalletAccountId == id);
            bool hasTransfers = await _context.AccountTransfers.AnyAsync(i => i.FromWalletAccountId == id || i.ToWalletAccountId == id);
            if (hasTransactions || hasTransfers)
            {
                TempData["ErrorMessage"] = "Account cannot be deleted because it has transaction or transfer history.";
                return RedirectToAction(nameof(Index));
            }

            account.IsDeleted = true;
            account.DeletedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Account deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Transfers()
        {
            List<AccountTransfer> transferList = await _context.AccountTransfers
                .Include(i => i.FromWalletAccount)
                .Include(i => i.ToWalletAccount)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
            return View(transferList);
        }

        public IActionResult AddTransfer(int id = 0)
        {
            PopulateAccounts();
            if (id == 0)
            {
                return View(new AccountTransfer());
            }

            AccountTransfer? transfer = _context.AccountTransfers.FirstOrDefault(i => i.AccountTransferId == id);
            if (transfer == null)
            {
                return NotFound();
            }

            return View(transfer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTransfer([Bind("AccountTransferId,FromWalletAccountId,ToWalletAccountId,Amount,Date,Note")] AccountTransfer transfer)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Challenge();
            }

            transfer.UserId = CurrentUserId;
            ModelState.Remove(nameof(AccountTransfer.UserId));

            if (transfer.FromWalletAccountId == transfer.ToWalletAccountId)
            {
                ModelState.AddModelError(nameof(AccountTransfer.ToWalletAccountId), "Source and destination accounts must be different.");
            }

            bool fromExists = await _context.WalletAccounts.AnyAsync(i => i.WalletAccountId == transfer.FromWalletAccountId);
            bool toExists = await _context.WalletAccounts.AnyAsync(i => i.WalletAccountId == transfer.ToWalletAccountId);
            if (!fromExists)
            {
                ModelState.AddModelError(nameof(AccountTransfer.FromWalletAccountId), "Please select a valid source account.");
            }
            if (!toExists)
            {
                ModelState.AddModelError(nameof(AccountTransfer.ToWalletAccountId), "Please select a valid destination account.");
            }

            if (ModelState.IsValid)
            {
                if (transfer.AccountTransferId == 0)
                {
                    _context.AccountTransfers.Add(transfer);
                }
                else
                {
                    AccountTransfer? transferToUpdate = await _context.AccountTransfers
                        .FirstOrDefaultAsync(i => i.AccountTransferId == transfer.AccountTransferId);
                    if (transferToUpdate == null)
                    {
                        return NotFound();
                    }

                    transferToUpdate.FromWalletAccountId = transfer.FromWalletAccountId;
                    transferToUpdate.ToWalletAccountId = transfer.ToWalletAccountId;
                    transferToUpdate.Amount = transfer.Amount;
                    transferToUpdate.Date = transfer.Date;
                    transferToUpdate.Note = transfer.Note;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Transfers));
            }

            PopulateAccounts();
            return View(transfer);
        }

        [HttpPost, ActionName("DeleteTransfer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransferConfirmed(int id)
        {
            AccountTransfer? transfer = await _context.AccountTransfers.FirstOrDefaultAsync(i => i.AccountTransferId == id);
            if (transfer != null)
            {
                transfer.IsDeleted = true;
                transfer.DeletedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Transfers));
        }

        [NonAction]
        public void PopulateAccounts()
        {
            List<WalletAccount> accounts = _context.WalletAccounts
                .OrderBy(i => i.Name)
                .ToList();
            WalletAccount defaultAccount = new() { WalletAccountId = 0, Name = "Choose an Account" };
            accounts.Insert(0, defaultAccount);
            ViewBag.WalletAccounts = accounts;
        }

        private static IReadOnlyList<string> GetAccountTypes()
        {
            return new List<string>
            {
                "Cash",
                "Bank Account",
                "Credit Card",
                "PayTM",
                "PhonePe",
                "PayPal",
                "Other"
            };
        }
    }
}
