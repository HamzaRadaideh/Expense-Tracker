using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Models;
using Expense_Tracker.Models.ExpenseTracker;

namespace Expense_Tracker.Controllers
{
    public class DashboardController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            // Get the current user's ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Define date range for the last 30 days
            DateTime StartDate = DateTime.Today.AddDays(-29); // 30 days including today
            DateTime EndDate = DateTime.Today;

            // Retrieve user-specific transactions within the last 30 days
            List<Transaction> userTransactions = await context.Transactions
                .Include(x => x.Category)
                .Where(y => y.UserId == userId && y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;

            // Total Income Calculation
            int totalIncome = userTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = totalIncome.ToString("C0", culture);

            // Total Expense Calculation
            int totalExpense = userTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = totalExpense.ToString("C0", culture);

            // Balance Calculation
            int balance = totalIncome - totalExpense;
            ViewBag.Balance = balance.ToString("C0", culture);

            // Doughnut Chart Data - Expenses by Category
            ViewBag.DoughnutChartData = userTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0", culture),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            // Spline Chart Data - Income vs Expense over the last 30 days
            var incomeSummary = userTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            var expenseSummary = userTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            // Generate data for each of the last 30 days, filling missing dates with 0s
            string[] last30Days = Enumerable.Range(0, 30)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in last30Days
                                      join income in incomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in expenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income?.income ?? 0,
                                          expense = expense?.expense ?? 0,
                                      };

            // Retrieve and format the 5 most recent transactions
            var recentTransactions = await context.Transactions
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentTransactions = recentTransactions.Select(item => new
            {
                CategoryTitleWithIcon = item.Category != null ? item.Category.Icon + " " + item.Category.Title : "No Category",
                Date = item.Date.ToString("MMM-dd-yy"),
                FormattedAmount = item.Amount.ToString("C0", culture),
                item.Note
            }).ToList();


            return View();
        }


        public class SplineChartData
        {
            public string day;
            public int income;
            public int expense;
        }
    }
}
