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
            await SeedDefaultAccountsForUser(user.Id);

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
                new() { UserId = userId, Title = "Salary", Icon = "fa-solid fa-wallet", Type = "Income" },
                new() { UserId = userId, Title = "Reimbursements", Icon = "fa-solid fa-hand-holding-dollar", Type = "Income" },
                new() { UserId = userId, Title = "Stocks", Icon = "fa-solid fa-chart-line", Type = "Income" },
                new() { UserId = userId, Title = "Rent", Icon = "fa-solid fa-house", Type = "Expense", MonthlyBudget = 25000 },
                new() { UserId = userId, Title = "Travel", Icon = "fa-solid fa-plane", Type = "Expense", MonthlyBudget = 6000 },
                new() { UserId = userId, Title = "Food", Icon = "fa-solid fa-utensils", Type = "Expense", MonthlyBudget = 8000 },
                new() { UserId = userId, Title = "Shopping", Icon = "fa-solid fa-basket-shopping", Type = "Expense", MonthlyBudget = 5000 },
                new() { UserId = userId, Title = "Utilities", Icon = "fa-solid fa-bolt", Type = "Expense", MonthlyBudget = 4000 }
            };

            _context.Categories.AddRange(defaultCategories);
            await _context.SaveChangesAsync();
        }

        private async Task SeedDefaultAccountsForUser(string userId)
        {
            var defaultAccounts = new List<WalletAccount>
            {
                new() { UserId = userId, Name = "Cash", Type = "Cash", OpeningBalance = 0 },
                new() { UserId = userId, Name = "Bank Account", Type = "Bank Account", OpeningBalance = 0 },
                new() { UserId = userId, Name = "Credit Card", Type = "Credit Card", OpeningBalance = 0, CreditLimit = 50000, DueDate = DateTime.Today.AddDays(15) },
                new() { UserId = userId, Name = "PayTM", Type = "PayTM", OpeningBalance = 0 },
                new() { UserId = userId, Name = "PhonePe", Type = "PhonePe", OpeningBalance = 0 },
                new() { UserId = userId, Name = "PayPal", Type = "PayPal", OpeningBalance = 0 }
            };

            _context.WalletAccounts.AddRange(defaultAccounts);
            await _context.SaveChangesAsync();
        }
    }
}
