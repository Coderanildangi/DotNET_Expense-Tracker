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
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // GET: Category
        public async Task<IActionResult> Index()
        {
            return _context.Categories != null ?
                        View(await _context.Categories.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
        }


        // GET: Category/AddOrEdit
        public IActionResult AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View(new Category());
            else
            {
                var category = _context.Categories.FirstOrDefault(i => i.CategoryId == id);
                if (category == null)
                {
                    return NotFound();
                }

                return View(category);
            }

        }

        // POST: Category/AddOrEdit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("CategoryId,Title,Icon,Type,MonthlyBudget")] Category category)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Challenge();
            }

            if (category.Type == "Income")
            {
                category.MonthlyBudget = null;
            }

            if (ModelState.IsValid)
            {
                if (category.CategoryId == 0)
                {
                    category.UserId = CurrentUserId;
                    _context.Add(category);
                }
                else
                {
                    var categoryToUpdate = await _context.Categories.FirstOrDefaultAsync(i => i.CategoryId == category.CategoryId);
                    if (categoryToUpdate == null)
                    {
                        return NotFound();
                    }

                    categoryToUpdate.Title = category.Title;
                    categoryToUpdate.Icon = category.Icon;
                    categoryToUpdate.Type = category.Type;
                    categoryToUpdate.MonthlyBudget = category.MonthlyBudget;
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }


        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Categories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
            }

            var category = await _context.Categories.FirstOrDefaultAsync(i => i.CategoryId == id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Category was not found.";
                return RedirectToAction(nameof(Index));
            }

            bool categoryInUse = await _context.Transactions.AnyAsync(i => i.CategoryId == id);
            if (categoryInUse)
            {
                TempData["ErrorMessage"] = "Category cannot be deleted because it has transactions.";
                return RedirectToAction(nameof(Index));
            }

            category.IsDeleted = true;
            category.DeletedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

    }
}
