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
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Transactions.Include(t => t.Category);
            return View(await applicationDbContext.ToListAsync());
        }

      // GET: Transaction/AddOrEdit
      public IActionResult AddOrEdit(int id = 0)
      {
         if (!ModelState.IsValid)
         {
            PopulateCategories();
            return View(new Transaction());
         }

         PopulateCategories();
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
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,Amount,Note,Date")] Transaction transaction)
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
                    transactionToUpdate.Amount = transaction.Amount;
                    transactionToUpdate.Note = transaction.Note;
                    transactionToUpdate.Date = transaction.Date;
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateCategories();
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
    }
}
