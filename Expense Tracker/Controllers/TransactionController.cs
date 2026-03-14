using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Models;
using System.Security.Claims;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // GET: Transaction
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            int? categoryId,
            int? minAmount,
            int? maxAmount,
            int? accountId,
            string? transaction)
        {
            IQueryable<Transaction> query = _context.Transactions
                .Include(i => i.Category)
                .Include(i => i.WalletAccount);

            if (startDate.HasValue)
            {
                DateTime normalizedStart = startDate.Value.Date;
                query = query.Where(i => i.Date.Date >= normalizedStart);
            }

            if (endDate.HasValue)
            {
                DateTime normalizedEnd = endDate.Value.Date;
                query = query.Where(i => i.Date.Date <= normalizedEnd);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            if (minAmount.HasValue)
            {
                query = query.Where(i => i.Amount >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                query = query.Where(i => i.Amount <= maxAmount.Value);
            }

            if (accountId.HasValue && accountId.Value > 0)
            {
                query = query.Where(i => i.WalletAccountId == accountId.Value);
            }

            if (!string.IsNullOrWhiteSpace(transaction))
            {
                string normalizedTransaction = transaction.Trim();
                bool transactionIdParsed = int.TryParse(normalizedTransaction, out int parsedTransactionId);
                query = query.Where(i =>
                    (i.Note != null && EF.Functions.Like(i.Note, $"%{normalizedTransaction}%")) ||
                    (i.Category != null && EF.Functions.Like(i.Category.Title, $"%{normalizedTransaction}%")) ||
                    (i.WalletAccount != null && EF.Functions.Like(i.WalletAccount.Name, $"%{normalizedTransaction}%")) ||
                    (transactionIdParsed && i.TransactionId == parsedTransactionId));
            }

            ViewBag.FilterCategories = await _context.Categories
                .OrderBy(i => i.Title)
                .ToListAsync();

            ViewBag.FilterAccounts = await _context.WalletAccounts
                .OrderBy(i => i.Name)
                .ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.CategoryId = categoryId;
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;
            ViewBag.AccountId = accountId;
            ViewBag.Transaction = transaction;

            List<Transaction> transactions = await query
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.TransactionId)
                .ToListAsync();

            return View(transactions);
        }

      // GET: Transaction/AddOrEdit
      public IActionResult AddOrEdit(int id = 0)
      {
         if (!ModelState.IsValid)
         {
            PopulateCategories();
            PopulateAccounts();
            return View(new Transaction());
         }

         PopulateCategories();
         PopulateAccounts();
         if (id == 0)
            return View(new Transaction());
         else
         {
            var transaction = _context.Transactions.FirstOrDefault(i => i.TransactionId == id);
            if (transaction == null)
            {
               return NotFound();
            }

            return View(transaction);
         }
      }

        // POST: Transaction/AddOrEdit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,WalletAccountId,Amount,Note,Date")] Transaction transaction)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Challenge();
            }
            
            // UserId is assigned server-side, so don't fail model validation for an unbound form field.
            transaction.UserId = CurrentUserId;
            ModelState.Remove(nameof(Transaction.UserId));

            bool categoryExists = await _context.Categories.AnyAsync(i => i.CategoryId == transaction.CategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(Transaction.CategoryId), "Please select a valid category.");
            }

            if (!transaction.WalletAccountId.HasValue || transaction.WalletAccountId.Value <= 0)
            {
                ModelState.AddModelError(nameof(Transaction.WalletAccountId), "Please select an account.");
            }
            else
            {
                bool accountExists = await _context.WalletAccounts.AnyAsync(i => i.WalletAccountId == transaction.WalletAccountId.Value);
                if (!accountExists)
                {
                    ModelState.AddModelError(nameof(Transaction.WalletAccountId), "Please select a valid account.");
                }
            }

            if (ModelState.IsValid)
            {
                if (transaction.TransactionId == 0)
                {
                    _context.Add(transaction);
                }
                else
                {
                    var transactionToUpdate = await _context.Transactions.FirstOrDefaultAsync(i => i.TransactionId == transaction.TransactionId);
                    if (transactionToUpdate == null)
                    {
                        return NotFound();
                    }

                    transactionToUpdate.CategoryId = transaction.CategoryId;
                    transactionToUpdate.WalletAccountId = transaction.WalletAccountId;
                    transactionToUpdate.Amount = transaction.Amount;
                    transactionToUpdate.Note = transaction.Note;
                    transactionToUpdate.Date = transaction.Date;
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateCategories();
            PopulateAccounts();
            return View(transaction);
        }

        // POST: Transaction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Transactions == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Transactions'  is null.");
            }
            var transaction = await _context.Transactions.FirstOrDefaultAsync(i => i.TransactionId == id);
            if (transaction != null)
            {
                transaction.IsDeleted = true;
                transaction.DeletedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [NonAction]
        public void PopulateCategories()
        {
            var CategoryCollection = _context.Categories.ToList();
            Category DefaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
            CategoryCollection.Insert(0, DefaultCategory);
            ViewBag.Categories = CategoryCollection;
        }

        [NonAction]
        public void PopulateAccounts()
        {
            var accountCollection = _context.WalletAccounts.OrderBy(i => i.Name).ToList();
            WalletAccount defaultAccount = new() { WalletAccountId = 0, Name = "Choose an Account" };
            accountCollection.Insert(0, defaultAccount);
            ViewBag.WalletAccounts = accountCollection;
        }
    }
}
