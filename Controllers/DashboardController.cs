using Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;

namespace Expense_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ActionResult> Index(string months, string accounts, string years)
        {
            int[] selectedMonths;
            int[] selectedYears;
            string[] selectedMonthsStr;
            int[] accountsInt;
            if (months!= null)
            {
                selectedMonthsStr = months.Split(',');
                string[] monthNamesInOrder = new string[]
                {
                    "January", "February", "March", "April",
                    "May", "June", "July", "August",
                    "September", "October", "November", "December"
                };

                // Sort the selected months based on their order in the monthNamesInOrder array
                selectedMonthsStr = selectedMonthsStr
                    .OrderBy(monthName => Array.IndexOf(monthNamesInOrder, monthName))
                    .ToArray();
                // Define a mapping of month names to month numbers
                var monthNameToNumber = new Dictionary<string, int>
                {
                    { "January", 1 },
                    { "February", 2 },
                    { "March", 3 },
                    { "April", 4 },
                    { "May", 5 },
                    { "June", 6 },
                    { "July", 7 },
                    { "August", 8 },
                    { "September", 9 },
                    { "October", 10 },
                    { "November", 11 },
                    { "December", 12 }
                };

                // Convert the selected month names to month numbers
                selectedMonths = selectedMonthsStr.Select(monthName =>
                {
                    if (monthNameToNumber.TryGetValue(monthName, out int monthNumber))
                    {
                        return monthNumber;
                    }
                    return -1; // Return -1 for invalid month names (you can handle this differently if needed)
                }).Where(monthNumber => monthNumber != -1).ToArray();
            }
            else
            {
                selectedMonths = new int[] {1,2,3,4,5,6,7,8,9,10,11,12};
                selectedMonthsStr = new string[] {"all"};
            }
            if (accounts!=null)
            {
                string[] accountsStrings = accounts.Split(",");
                // Convert the substrings to integers and store them in an integer array
                accountsInt = accountsStrings.Select(int.Parse).ToArray();
            }
            else
            {
                accountsInt = await _context.Account
                            .Select(account => account.AccountId)
                            .ToArrayAsync();
            }
            if (years!=null)
            {
                selectedYears = years.Split(',').Select(int.Parse).OrderBy(year => year).ToArray();
            }
            else
            {
                selectedYears = await _context.Transactions
                            .Select(transaction => transaction.Date.Year)
                            .Distinct()
                            .ToArrayAsync();
            }

            //int selectedYear = DateTime.Today.Year;

            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate= DateTime.Today;
            List<Transaction> SelectedTransactions = new List<Transaction>();

            SelectedTransactions = await _context.Transactions
            .Include(x => x.Category)
            .Where(y => selectedMonths.Contains(y.Date.Month) && selectedYears.Contains(y.Date.Year) 
            && accountsInt.Contains(y.AccountId))
            .ToListAsync();

            decimal TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C2");

            decimal TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C2");

            decimal Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C2}", Balance);

            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon+" "+k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C2"),
                })
                .OrderByDescending(l=>l.amount)
                .ToList();

            CultureInfo englishCulture = new CultureInfo("en-US");
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i=>i.Category.Type == "Income")
                .GroupBy(j=>j.Date)
                .Select(k=> new SplineChartData()
                {
                    day= k.First().Date.ToString("dd-MMM-yy", englishCulture),
                    income = k.Sum(l=>l.Amount)
                })
                .ToList();
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM-yy", englishCulture),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            string[] days;

            List<string> selectedMonthDates = new List<string>();
            // Loop through the selected months
            foreach (int selectedYear in selectedYears) {
                foreach (int selectedMonth in selectedMonths)
                {
                    // Calculate the first day of the month for the selected year and month
                    DateTime firstDayOfMonth = new DateTime(selectedYear, selectedMonth, 1);
                    // Calculate the last day of the month by moving one month forward and subtracting one day
                    DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                    // Loop through the days of the month and add date strings to the array
                    for (DateTime date = firstDayOfMonth; date <= lastDayOfMonth; date = date.AddDays(1))
                    {
                        selectedMonthDates.Add(date.ToString("dd-MMM-yy", englishCulture));
                    }
                } 
            }
            string[] selectedMonthDatesArray = selectedMonthDates.ToArray();
            days= selectedMonthDatesArray;
            

            ViewBag.SplineChartData = from day in days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into dayExpenseJoined
                                      from expense in dayExpenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(j => j.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            ViewBag.SelectedMonths = string.Join(" ", selectedMonthsStr);
            ViewBag.SelectedAccounts = string.Join(" ",await _context.Account.Where(y=>accountsInt.Contains(y.AccountId))
                            .Select(account => account.Title)
                            .ToArrayAsync());

            var AccountCollection = await _context.Account.ToListAsync();
            Account DefaultAccount = new Account() { AccountId = 0, Title = "Choose an Account" };
            AccountCollection.Insert(0, DefaultAccount);
            ViewBag.Accounts = AccountCollection;
            ViewBag.years = _context.Transactions
                            .Select(transaction => transaction.Date.Year)
                            .Distinct()
                            .ToList();

            return View();
        }
        public class SplineChartData
        {
            public string day;
            public decimal income;
            public decimal expense;
        }
    }
}
