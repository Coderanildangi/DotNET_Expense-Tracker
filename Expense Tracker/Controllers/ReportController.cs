using Expense_Tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly IReadOnlyList<ReportMenuItem> ReportMenu = new List<ReportMenuItem>
        {
            new() { Key = "daily-expenses", Label = "Daily expenses" },
            new() { Key = "monthly-summary", Label = "Monthly summary" },
            new() { Key = "category-breakdown", Label = "Category breakdown" },
            new() { Key = "income-vs-expense", Label = "Income vs expense" },
            new() { Key = "yearly-trends", Label = "Yearly trends" },
            new() { Key = "account-balances", Label = "Account balances" },
            new() { Key = "transfer-history", Label = "Transfer history" },
            new() { Key = "credit-card-dues", Label = "Credit card dues" }
        };

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string reportType = "daily-expenses")
        {
            ReportTableData reportData = await BuildReportAsync(reportType);
            var viewModel = new ReportViewModel
            {
                Reports = ReportMenu,
                SelectedReportType = reportData.Key,
                SelectedReportTitle = reportData.Title,
                Headers = reportData.Headers,
                Rows = reportData.Rows
            };

            ViewData["PageTitle"] = "Reports & Analytics";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Export(string reportType, string format)
        {
            ReportTableData reportData = await BuildReportAsync(reportType);
            string normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();
            string dateSuffix = DateTime.Today.ToString("yyyyMMdd");

            if (normalizedFormat == "csv")
            {
                byte[] csvContent = BuildCsv(reportData);
                return File(csvContent, "text/csv", $"{reportData.Key}-{dateSuffix}.csv");
            }

            if (normalizedFormat == "excel")
            {
                byte[] excelContent = BuildExcelHtml(reportData);
                return File(excelContent, "application/vnd.ms-excel", $"{reportData.Key}-{dateSuffix}.xls");
            }

            if (normalizedFormat == "pdf")
            {
                byte[] pdfContent = BuildSimplePdf(reportData);
                return File(pdfContent, "application/pdf", $"{reportData.Key}-{dateSuffix}.pdf");
            }

            return BadRequest("Unsupported export format. Allowed values: pdf, excel, csv.");
        }

        private async Task<ReportTableData> BuildReportAsync(string reportType)
        {
            string normalizedReportType = NormalizeReportType(reportType);
            DateTime today = DateTime.Today;

            List<Transaction> transactions = await _context.Transactions
                .Include(i => i.Category)
                .Include(i => i.WalletAccount)
                .ToListAsync();
            List<WalletAccount> walletAccounts = await _context.WalletAccounts.ToListAsync();
            List<AccountTransfer> transfers = await _context.AccountTransfers
                .Include(i => i.FromWalletAccount)
                .Include(i => i.ToWalletAccount)
                .ToListAsync();

            return normalizedReportType switch
            {
                "monthly-summary" => BuildMonthlySummaryReport(transactions, today),
                "category-breakdown" => BuildCategoryBreakdownReport(transactions, today),
                "income-vs-expense" => BuildIncomeVsExpenseReport(transactions, today),
                "yearly-trends" => BuildYearlyTrendsReport(transactions, today),
                "account-balances" => BuildAccountBalancesReport(walletAccounts, transactions, transfers),
                "transfer-history" => BuildTransferHistoryReport(transfers, today),
                "credit-card-dues" => BuildCreditCardDuesReport(walletAccounts, transactions, transfers),
                _ => BuildDailyExpensesReport(transactions, today)
            };
        }

        private static string NormalizeReportType(string reportType)
        {
            if (string.IsNullOrWhiteSpace(reportType))
            {
                return "daily-expenses";
            }

            bool exists = ReportMenu.Any(i => i.Key.Equals(reportType, StringComparison.OrdinalIgnoreCase));
            return exists ? reportType.ToLowerInvariant() : "daily-expenses";
        }

        private static ReportTableData BuildDailyExpensesReport(IEnumerable<Transaction> transactions, DateTime today)
        {
            List<IReadOnlyList<string>> rows = transactions
                .Where(i => i.Category?.Type == "Expense" && i.Date.Date == today.Date)
                .OrderByDescending(i => i.Date)
                .Select(i => (IReadOnlyList<string>)new List<string>
                {
                    i.Date.ToString("dd-MMM-yyyy"),
                    i.Category?.Title ?? "Unknown",
                    string.IsNullOrWhiteSpace(i.Note) ? "-" : i.Note!,
                    i.Amount.ToString("C0")
                })
                .ToList();

            return new ReportTableData
            {
                Key = "daily-expenses",
                Title = "Daily expenses",
                Headers = new List<string> { "Date", "Category", "Note", "Amount" },
                Rows = rows
            };
        }

        private static ReportTableData BuildMonthlySummaryReport(IEnumerable<Transaction> transactions, DateTime today)
        {
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            List<IReadOnlyList<string>> rows = transactions
                .Where(i => i.Date.Date >= monthStart && i.Date.Date <= monthEnd)
                .GroupBy(i => i.Date.Date)
                .OrderBy(i => i.Key)
                .Select(i =>
                {
                    int income = i.Where(x => x.Category?.Type == "Income").Sum(x => x.Amount);
                    int expense = i.Where(x => x.Category?.Type == "Expense").Sum(x => x.Amount);
                    int net = income - expense;

                    return (IReadOnlyList<string>)new List<string>
                    {
                        i.Key.ToString("dd-MMM-yyyy"),
                        income.ToString("C0"),
                        expense.ToString("C0"),
                        net.ToString("C0")
                    };
                })
                .ToList();

            return new ReportTableData
            {
                Key = "monthly-summary",
                Title = "Monthly summary",
                Headers = new List<string> { "Date", "Income", "Expense", "Net" },
                Rows = rows
            };
        }

        private static ReportTableData BuildCategoryBreakdownReport(IEnumerable<Transaction> transactions, DateTime today)
        {
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            List<IReadOnlyList<string>> rows = transactions
                .Where(i => i.Date.Date >= monthStart && i.Date.Date <= monthEnd)
                .GroupBy(i => new
                {
                    CategoryName = i.Category?.Title ?? "Unknown",
                    CategoryType = i.Category?.Type ?? "Expense"
                })
                .OrderByDescending(i => i.Sum(x => x.Amount))
                .Select(i => (IReadOnlyList<string>)new List<string>
                {
                    i.Key.CategoryName,
                    i.Key.CategoryType,
                    i.Count().ToString(CultureInfo.InvariantCulture),
                    i.Sum(x => x.Amount).ToString("C0")
                })
                .ToList();

            return new ReportTableData
            {
                Key = "category-breakdown",
                Title = "Category breakdown",
                Headers = new List<string> { "Category", "Type", "Transactions", "Total" },
                Rows = rows
            };
        }

        private static ReportTableData BuildIncomeVsExpenseReport(IEnumerable<Transaction> transactions, DateTime today)
        {
            DateTime startDate = today.AddDays(-29);
            string[] last30Days = Enumerable.Range(0, 30)
                .Select(i => startDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            var incomeByDay = transactions
                .Where(i => i.Date.Date >= startDate && i.Date.Date <= today && i.Category?.Type == "Income")
                .GroupBy(i => i.Date.Date)
                .ToDictionary(i => i.Key.ToString("dd-MMM"), i => i.Sum(x => x.Amount));

            var expenseByDay = transactions
                .Where(i => i.Date.Date >= startDate && i.Date.Date <= today && i.Category?.Type == "Expense")
                .GroupBy(i => i.Date.Date)
                .ToDictionary(i => i.Key.ToString("dd-MMM"), i => i.Sum(x => x.Amount));

            List<IReadOnlyList<string>> rows = last30Days
                .Select(day =>
                {
                    int income = incomeByDay.TryGetValue(day, out int incomeValue) ? incomeValue : 0;
                    int expense = expenseByDay.TryGetValue(day, out int expenseValue) ? expenseValue : 0;
                    int difference = income - expense;

                    return (IReadOnlyList<string>)new List<string>
                    {
                        day,
                        income.ToString("C0"),
                        expense.ToString("C0"),
                        difference.ToString("C0")
                    };
                })
                .ToList();

            return new ReportTableData
            {
                Key = "income-vs-expense",
                Title = "Income vs expense",
                Headers = new List<string> { "Day", "Income", "Expense", "Difference" },
                Rows = rows
            };
        }

        private static ReportTableData BuildYearlyTrendsReport(IEnumerable<Transaction> transactions, DateTime today)
        {
            int year = today.Year;
            string[] monthNames = Enumerable.Range(1, 12)
                .Select(i => new DateTime(year, i, 1).ToString("MMM"))
                .ToArray();

            var incomeByMonth = transactions
                .Where(i => i.Date.Year == year && i.Category?.Type == "Income")
                .GroupBy(i => i.Date.Month)
                .ToDictionary(i => i.Key, i => i.Sum(x => x.Amount));

            var expenseByMonth = transactions
                .Where(i => i.Date.Year == year && i.Category?.Type == "Expense")
                .GroupBy(i => i.Date.Month)
                .ToDictionary(i => i.Key, i => i.Sum(x => x.Amount));

            List<IReadOnlyList<string>> rows = Enumerable.Range(1, 12)
                .Select(month =>
                {
                    int income = incomeByMonth.TryGetValue(month, out int incomeValue) ? incomeValue : 0;
                    int expense = expenseByMonth.TryGetValue(month, out int expenseValue) ? expenseValue : 0;
                    int balance = income - expense;

                    return (IReadOnlyList<string>)new List<string>
                    {
                        monthNames[month - 1],
                        income.ToString("C0"),
                        expense.ToString("C0"),
                        balance.ToString("C0")
                    };
                })
                .ToList();

            return new ReportTableData
            {
                Key = "yearly-trends",
                Title = "Yearly trends",
                Headers = new List<string> { "Month", "Income", "Expense", "Balance" },
                Rows = rows
            };
        }

        private static ReportTableData BuildAccountBalancesReport(
            IReadOnlyList<WalletAccount> walletAccounts,
            IReadOnlyList<Transaction> transactions,
            IReadOnlyList<AccountTransfer> transfers)
        {
            List<IReadOnlyList<string>> rows = walletAccounts
                .Select(account =>
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
                    int balance = account.OpeningBalance + income - expense + incomingTransfers - outgoingTransfers;

                    return (IReadOnlyList<string>)new List<string>
                    {
                        account.Name,
                        account.Type,
                        account.OpeningBalance.ToString("C0"),
                        income.ToString("C0"),
                        expense.ToString("C0"),
                        balance.ToString("C0")
                    };
                })
                .OrderBy(i => i[0], StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ReportTableData
            {
                Key = "account-balances",
                Title = "Account balances",
                Headers = new List<string> { "Account", "Type", "Opening", "Income", "Expense", "Current Balance" },
                Rows = rows
            };
        }

        private static ReportTableData BuildTransferHistoryReport(IReadOnlyList<AccountTransfer> transfers, DateTime today)
        {
            DateTime startDate = today.AddDays(-29);
            List<IReadOnlyList<string>> rows = transfers
                .Where(i => i.Date.Date >= startDate && i.Date.Date <= today)
                .OrderByDescending(i => i.Date)
                .Select(i => (IReadOnlyList<string>)new List<string>
                {
                    i.Date.ToString("dd-MMM-yyyy"),
                    i.FromWalletAccount?.Name ?? "Unknown",
                    i.ToWalletAccount?.Name ?? "Unknown",
                    i.Amount.ToString("C0"),
                    string.IsNullOrWhiteSpace(i.Note) ? "-" : i.Note!
                })
                .ToList();

            return new ReportTableData
            {
                Key = "transfer-history",
                Title = "Transfer history (last 30 days)",
                Headers = new List<string> { "Date", "From", "To", "Amount", "Note" },
                Rows = rows
            };
        }

        private static ReportTableData BuildCreditCardDuesReport(
            IReadOnlyList<WalletAccount> walletAccounts,
            IReadOnlyList<Transaction> transactions,
            IReadOnlyList<AccountTransfer> transfers)
        {
            List<IReadOnlyList<string>> rows = walletAccounts
                .Where(i => i.IsCreditCard)
                .Select(account =>
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
                    int balance = account.OpeningBalance + income - expense + incomingTransfers - outgoingTransfers;
                    int dueAmount = balance < 0 ? Math.Abs(balance) : 0;

                    return (IReadOnlyList<string>)new List<string>
                    {
                        account.Name,
                        dueAmount.ToString("C0"),
                        account.CreditLimit.HasValue ? account.CreditLimit.Value.ToString("C0") : "-",
                        account.DueDate.HasValue ? account.DueDate.Value.ToString("dd-MMM-yyyy") : "-"
                    };
                })
                .OrderBy(i => i[0], StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ReportTableData
            {
                Key = "credit-card-dues",
                Title = "Credit card dues",
                Headers = new List<string> { "Card", "Due Amount", "Credit Limit", "Due Date" },
                Rows = rows
            };
        }

        private static byte[] BuildCsv(ReportTableData reportData)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", reportData.Headers.Select(EscapeCsvCell)));

            foreach (IReadOnlyList<string> row in reportData.Rows)
            {
                builder.AppendLine(string.Join(",", row.Select(EscapeCsvCell)));
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        private static string EscapeCsvCell(string value)
        {
            string safeValue = value.Replace("\"", "\"\"");
            return $"\"{safeValue}\"";
        }

        private static byte[] BuildExcelHtml(ReportTableData reportData)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body>");
            builder.AppendLine($"<h3>{WebUtility.HtmlEncode(reportData.Title)}</h3>");
            builder.AppendLine("<table border=\"1\" cellspacing=\"0\" cellpadding=\"4\">");
            builder.AppendLine("<thead><tr>");

            foreach (string header in reportData.Headers)
            {
                builder.AppendLine($"<th>{WebUtility.HtmlEncode(header)}</th>");
            }

            builder.AppendLine("</tr></thead><tbody>");

            foreach (IReadOnlyList<string> row in reportData.Rows)
            {
                builder.AppendLine("<tr>");
                foreach (string cell in row)
                {
                    builder.AppendLine($"<td>{WebUtility.HtmlEncode(cell)}</td>");
                }

                builder.AppendLine("</tr>");
            }

            builder.AppendLine("</tbody></table></body></html>");
            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        private static byte[] BuildSimplePdf(ReportTableData reportData)
        {
            var lines = new List<string>
            {
                reportData.Title,
                $"Generated on: {DateTime.Now:dd-MMM-yyyy HH:mm}",
                string.Empty,
                string.Join(" | ", reportData.Headers)
            };

            lines.AddRange(reportData.Rows.Select(i => string.Join(" | ", i)));
            List<string> pdfLines = lines.Take(45).ToList();

            var contentBuilder = new StringBuilder();
            contentBuilder.AppendLine("BT");
            contentBuilder.AppendLine("/F1 14 Tf");
            contentBuilder.AppendLine("50 770 Td");
            contentBuilder.AppendLine($"({EscapePdfText(pdfLines.FirstOrDefault() ?? reportData.Title)}) Tj");
            contentBuilder.AppendLine("0 -22 Td");
            contentBuilder.AppendLine("/F1 10 Tf");

            foreach (string line in pdfLines.Skip(1))
            {
                contentBuilder.AppendLine($"({EscapePdfText(line)}) Tj");
                contentBuilder.AppendLine("T*");
            }

            contentBuilder.AppendLine("ET");
            string contentStream = contentBuilder.ToString();

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n{contentStream}endstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
            };

            using var stream = new MemoryStream();
            WriteAscii(stream, "%PDF-1.4\n");

            var offsets = new List<long> { 0 };
            for (int i = 0; i < objects.Count; i++)
            {
                offsets.Add(stream.Position);
                WriteAscii(stream, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            long xrefPosition = stream.Position;
            WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
            WriteAscii(stream, "0000000000 65535 f \n");
            foreach (long offset in offsets.Skip(1))
            {
                WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
            }

            WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
            return stream.ToArray();
        }

        private static string EscapePdfText(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }

        private static void WriteAscii(Stream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
