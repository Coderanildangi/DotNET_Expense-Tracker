using Expense_Tracker.Models;
using Expense_Tracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName ?? model.Email,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var createUserResult = await _userManager.CreateAsync(user, model.Password);
            if (!createUserResult.Succeeded)
            {
                foreach (var error in createUserResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await SeedDefaultCategoriesForUser(user.Id);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        private async Task SeedDefaultCategoriesForUser(string userId)
        {
            var defaultCategories = new List<Category>
            {
                new() { UserId = userId, Title = "Salary", Icon = "S", Type = "Income" },
                new() { UserId = userId, Title = "Reimbursements", Icon = "R", Type = "Income" },
                new() { UserId = userId, Title = "Stocks", Icon = "ST", Type = "Income" },
                new() { UserId = userId, Title = "Rent", Icon = "RE", Type = "Expense", MonthlyBudget = 25000 },
                new() { UserId = userId, Title = "Travel", Icon = "TR", Type = "Expense", MonthlyBudget = 6000 },
                new() { UserId = userId, Title = "Food", Icon = "FO", Type = "Expense", MonthlyBudget = 8000 },
                new() { UserId = userId, Title = "Shopping", Icon = "SH", Type = "Expense", MonthlyBudget = 5000 },
                new() { UserId = userId, Title = "Utilities", Icon = "UT", Type = "Expense", MonthlyBudget = 4000 }
            };

            _context.Categories.AddRange(defaultCategories);
            await _context.SaveChangesAsync();
        }
    }
}
