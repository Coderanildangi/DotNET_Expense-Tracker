# Expense Tracker (ASP.NET Core MVC)
A personal expense tracking web app built with ASP.NET Core MVC, Entity Framework Core, Identity, and Syncfusion UI components.
## Features
- User registration, login, and logout with ASP.NET Core Identity
- User-specific data isolation for categories and transactions
- Category management (income/expense) with monthly budget support
- Transaction management with validation
- Soft delete for categories and transactions
- Dashboard with:
  - Total income, expense, and balance
  - Expense-by-category doughnut chart
  - Income vs expense trend (last 10 days)
  - Monthly budget vs spending overview
  - Recent transactions
- Structured logging with Serilog (console, file, and SQL sink)
## Tech Stack
- ASP.NET Core MVC (`net10.0`)
- Entity Framework Core + SQL Server
- ASP.NET Core Identity
- Syncfusion EJ2 ASP.NET Core components
- Serilog
## Prerequisites
- .NET SDK 10+
- SQL Server (LocalDB/SQL Server Express/full SQL Server)
- (Optional) Visual Studio 2022+
## Getting Started
1. **Clone repository**
   ```bash
   git clone <your-repo-url>
   cd Expense-Tracker-App-in-Asp.Net-Core-MVC
2. Create environment file

- Inside Expense Tracker/, create .env:
    - SYNCFUSION_LICENSE=your_syncfusion_license_key
    - DB_CONNECTION=Server=YOUR_SERVER;Database=TransactionDB1;Trusted_Connection=True;MultipleActiveResultSets=True;
    - SERILOG_TABLE_NAME=ApplicationLogs

3. Restore packages
    ```bash
    dotnet restore "Expense Tracker/Expense Tracker.csproj"

4. Run the app
    ```bash
    dotnet run --project "Expense Tracker/Expense Tracker.csproj"

5. Open browser at:
    - http://localhost:5110 (default from launch profile)

## Database & Migrations
    The app automatically runs Database.Migrate() at startup.
    Existing migrations are included under Expense Tracker/Migrations.
    If you need manual migration commands:
        ```bash
        dotnet ef migrations add <MigrationName> --project "Expense Tracker/Expense Tracker.csproj"
        dotnet ef database update --project "Expense Tracker/Expense Tracker.csproj"

## Project Structure
Expense Tracker/
  Controllers/
    AccountController.cs
    DashboardController.cs
    CategoryController.cs
    TransactionController.cs
  Models/
    ApplicationDbContext.cs
    Category.cs
    Transaction.cs
    ViewModels/
  Views/
  Migrations/
  Program.cs
  appsettings.json
  .env

## Logging
Serilog is configured in Program.cs with sinks:

Console
Rolling file logs (Logs/log-.txt)
SQL Server table sink (SERILOG_TABLE_NAME, default: ApplicationLogs)

## Security Notes
Do not commit .env to source control.
Keep secrets (Syncfusion key, DB connection) in environment variables.
Rotate any key that was previously committed.

## Common Troubleshooting
Build error: file locked (Exceeded retry count ... apphost.exe)
A running app process is holding Expense Tracker.exe.

Fix:

taskkill /IM "Expense Tracker.exe" /F
dotnet build "Expense Tracker/Expense Tracker.csproj"
License
Add your preferred license details here.

If you want, I can also provide a shorter “portfolio-style” README version with screenshots and badges.

 