using DotNetEnv;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Expense_Tracker.Models
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            Env.Load();

            string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DB_CONNECTION environment variable is not set.");
            }

            if (!connectionString.Contains("TrustServerCertificate=", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += ";TrustServerCertificate=True";
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options, new HttpContextAccessor());
        }
    }
}
