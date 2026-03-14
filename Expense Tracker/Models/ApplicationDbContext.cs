using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }

        private string? CurrentUserId => _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasQueryFilter(i =>
                !i.IsDeleted &&
                i.UserId == CurrentUserId);

            modelBuilder.Entity<Transaction>().HasQueryFilter(i =>
                !i.IsDeleted &&
                i.UserId == CurrentUserId);

            modelBuilder.Entity<Category>()
                .HasIndex(i => i.UserId);

            modelBuilder.Entity<Transaction>()
                .HasIndex(i => i.UserId);
        }
    }
}
