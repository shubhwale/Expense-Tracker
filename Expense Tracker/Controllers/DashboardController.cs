using Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Expense_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<ActionResult> Index()
        {
            // Last 60 days
            DateTime StartDate = DateTime.Now.AddDays(-60);
            DateTime EndDate = DateTime.Now;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            // Total income transactions
            int TotalIncome = SelectedTransactions.Where(i => i.Category.Type == "Income").Sum(i => i.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            // Total expense transactions
            int TotalExpense = SelectedTransactions.Where(i => i.Category.Type == "Expense").Sum(i => i.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            // Balance
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = string.Format(culture, "{0:C0}", Balance);

            // Expense chart - expense by category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new 
                {
                    categoryTitleWithIcon = k.First().Category.TitleWithIcon,
                    amount = k.Sum(i => i.Amount),
                    formattedAmount = k.Sum(i => i.Amount).ToString("C0")
                }).OrderByDescending(l => l.amount).ToList();

            // Spine chart - income vs expense
            // Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData
                {
                    Day = k.First().Date.ToString("dd-MMM"),
                    Income = k.Sum(l => l.Amount)
                }).ToList();

            // Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData
                {
                    Day = k.First().Date.ToString("dd-MMM"),
                    Expense = k.Sum(l => l.Amount)
                }).ToList();

            // Combine income and expense
            string[] Last60days = Enumerable.Range(0, 60)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last60days
                                      join income in IncomeSummary on day equals income.Day into incomeJoined
                                      from income in incomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.Day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.Income,
                                          expense = expense == null ? 0 : expense.Expense,
                                      };

            // Recent transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string Day;
        public int Income;
        public int Expense;
    }
}
