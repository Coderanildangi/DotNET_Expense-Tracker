using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DotNetEnv;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
Env.Load();

// Retrieve values from the environment
var syncfusionLicense = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE");
var rawDbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? throw new InvalidOperationException("DB_CONNECTION environment variable is not set.");
var dbConnection = EnsureTrustedSqlConnectionString(rawDbConnection);
var logTableName = Environment.GetEnvironmentVariable("SERILOG_TABLE_NAME") ?? "ApplicationLogs";

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
   loggerConfiguration
      .MinimumLevel.Information()
      .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
      .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
      .Enrich.FromLogContext()
      .WriteTo.Console(
         outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
      .WriteTo.File(
         path: "Logs/log-.txt",
         rollingInterval: RollingInterval.Day,
         retainedFileCountLimit: 31,
         shared: true)
      .WriteTo.MSSqlServer(
         connectionString: dbConnection,
         sinkOptions: new MSSqlServerSinkOptions
         {
            TableName = logTableName,
            AutoCreateSqlTable = true
         },
         restrictedToMinimumLevel: LogEventLevel.Error);
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Dependency Injection for DBContext using .env connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(dbConnection));

builder.Services.AddHttpContextAccessor();

builder.Services
   .AddIdentity<ApplicationUser, IdentityRole>(options =>
   {
      options.User.RequireUniqueEmail = true;
      options.Password.RequiredLength = 6;
      options.Password.RequireDigit = false;
      options.Password.RequireUppercase = false;
      options.Password.RequireLowercase = false;
      options.Password.RequireNonAlphanumeric = false;
   })
   .AddEntityFrameworkStores<ApplicationDbContext>()
   .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
   options.LoginPath = "/Account/Login";
   options.AccessDeniedPath = "/Account/Login";
});

// Register Syncfusion license
if (!string.IsNullOrEmpty(syncfusionLicense))
{
   Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
}

var app = builder.Build();
app.Logger.LogInformation("Expense Tracker application is starting.");

// Ensure pending EF Core migrations are applied at startup.
using (var scope = app.Services.CreateScope())
{
   var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
   dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseExceptionHandler("/Home/Error");

app.Use(async (context, next) =>
{
   try
   {
      await next();
   }
   catch (Exception ex)
   {
      app.Logger.LogError(
         ex,
         "Unhandled exception for {RequestMethod} {RequestPath}",
         context.Request.Method,
         context.Request.Path);
      throw;
   }
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

static string EnsureTrustedSqlConnectionString(string connectionString)
{
   if (connectionString.Contains("TrustServerCertificate=", StringComparison.OrdinalIgnoreCase))
   {
      return connectionString;
   }

   return connectionString.EndsWith(';')
      ? $"{connectionString}TrustServerCertificate=True"
      : $"{connectionString};TrustServerCertificate=True";
}
