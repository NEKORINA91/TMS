using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models.Domain;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class FinancialsController : Controller
    {
        private readonly TuitionProContext _db;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] Categories =
            { "Utilities", "Maintenance", "Supplies", "Salaries", "Other" };
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxReceiptBytes = 5 * 1024 * 1024;

        public FinancialsController(TuitionProContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public IActionResult Index()
        {
            ViewBag.Categories = Categories;
            return View();
        }

        // active enrolments that owe a fee for the month starting at ms
        private IQueryable<Enrollment> ActiveFor(DateTime ms)
        {
            var me = ms.AddMonths(1);
            return _db.Enrollments.Where(e => e.IsActive && e.Class.IsActive && e.EnrolledDate < me);
        }

        private IQueryable<Enrollment> UnpaidFor(DateTime ms)
        {
            var me = ms.AddMonths(1);
            return ActiveFor(ms).Where(e => !e.Student.FeeCollections.Any(f =>
                f.ClassId == e.ClassId && f.Status == "Paid"
                && f.PeriodMonth >= ms && f.PeriodMonth < me));
        }

        // GET: /Financials/Summary?month=2026-11   (called by the page)
        public async Task<IActionResult> Summary(string? month)
        {
            if (!DateTime.TryParseExact((month ?? "") + "-01", "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var ms))
                ms = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var me = ms.AddMonths(1);
            var prev = ms.AddMonths(-1);
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // cash received and expenses logged inside the month
            var fees = await _db.FeeCollections
                .Where(f => f.Status == "Paid" && f.PaidAt >= ms && f.PaidAt < me)
                .Select(f => new { f.PaidAt, f.Amount }).ToListAsync();
            var exps = await _db.Expenses
                .Where(e => e.LoggedAt >= ms && e.LoggedAt < me)
                .Select(e => new { e.LoggedAt, e.Amount }).ToListAsync();

            int weeks = (DateTime.DaysInMonth(ms.Year, ms.Month) + 6) / 7;
            var rev = new decimal[weeks];
            var exp = new decimal[weeks];
            foreach (var f in fees) rev[(((DateTime)f.PaidAt).Day - 1) / 7] += f.Amount;
            foreach (var e in exps) exp[(e.LoggedAt.Day - 1) / 7] += e.Amount;
            var labels = Enumerable.Range(1, weeks).Select(i => "W" + i).ToArray();

            decimal todayRev = await _db.FeeCollections
                .Where(f => f.Status == "Paid" && f.PaidAt >= today && f.PaidAt < tomorrow)
                .SumAsync(f => (decimal?)f.Amount) ?? 0m;
            decimal todayExp = await _db.Expenses
                .Where(e => e.LoggedAt >= today && e.LoggedAt < tomorrow)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            // fee status for the month, counted per enrolment
            int totalEnrol = await ActiveFor(ms).CountAsync();
            int unpaidCount = await UnpaidFor(ms).CountAsync();
            decimal unpaidAmt = await UnpaidFor(ms).SumAsync(e => (decimal?)e.Class.MonthlyFee) ?? 0m;
            decimal paidAmt = await _db.FeeCollections
                .Where(f => f.Status == "Paid" && f.PeriodMonth >= ms && f.PeriodMonth < me)
                .SumAsync(f => (decimal?)f.Amount) ?? 0m;
            int overdueCount = await UnpaidFor(prev).CountAsync();
            decimal overdueAmt = await UnpaidFor(prev).SumAsync(e => (decimal?)e.Class.MonthlyFee) ?? 0m;

            var feeRows = await _db.FeeCollections
                .Where(f => f.PaidAt >= ms && f.PaidAt < me)
                .OrderByDescending(f => f.PaidAt)
                .Take(10)
                .Select(f => new
                {
                    f.ReceiptNo,
                    student = f.Student.User.FullName,
                    cls = f.Class.ClassName,
                    f.PaidAt,
                    f.Status,
                    f.Amount
                }).ToListAsync();

            var expRows = await _db.Expenses
                .Where(e => e.LoggedAt >= ms && e.LoggedAt < me)
                .OrderByDescending(e => e.LoggedAt)
                .Take(10)
                .Select(e => new { e.ExpenseId, e.Description, e.Category, e.Amount, e.LoggedAt, e.ReceiptPath })
                .ToListAsync();

            // estimated teacher payouts (centre keeps the commission rate)
            var gross = await _db.FeeCollections
                .Where(f => f.Status == "Paid" && f.PeriodMonth >= ms && f.PeriodMonth < me)
                .GroupBy(f => f.Class.TeacherId)
                .Select(g => new { TeacherId = g.Key, Gross = g.Sum(f => f.Amount) })
                .ToListAsync();
            var teachers = await _db.Teachers.Include(t => t.User).ToListAsync();
            var payouts = gross.Select(g =>
            {
                var t = teachers.First(x => x.TeacherId == g.TeacherId);
                decimal share = Math.Round(g.Gross * t.CommissionRate / 100m, 2);
                return new { name = t.User.FullName, gross = g.Gross, rate = t.CommissionRate, net = g.Gross - share };
            }).ToList();

            return Json(new
            {
                monthLabel = ms.ToString("MMMM yyyy"),
                todayRev,
                todayExp,
                monthRev = fees.Sum(f => f.Amount),
                monthExp = exps.Sum(e => e.Amount),
                labels,
                rev,
                exp,
                paidCount = totalEnrol - unpaidCount,
                paidAmt,
                pendingCount = unpaidCount,
                pendingAmt = unpaidAmt,
                overdueCount,
                overdueAmt,
                fees = feeRows,
                expenses = expRows,
                payouts
            });
        }

        // POST: /Financials/AddExpense   (called by the side form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(string description, decimal amount,
            string category, IFormFile? receipt)
        {
            if (string.IsNullOrWhiteSpace(description) || amount <= 0 || !Categories.Contains(category))
                return Json(new { ok = false, message = "Enter a description, a positive amount and a category." });

            string? receiptPath = null;
            if (receipt != null && receipt.Length > 0)
            {
                var ext = Path.GetExtension(receipt.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext) || receipt.Length > MaxReceiptBytes)
                    return Json(new { ok = false, message = "Receipt must be a PDF, JPG or PNG up to 5 MB." });

                var folder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid()}{ext}";
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                    await receipt.CopyToAsync(stream);
                receiptPath = $"/uploads/receipts/{fileName}";
            }

            var user = HttpContext.Session.GetSessionUser();
            _db.Expenses.Add(new Expense
            {
                AdminId = user!.ProfileId,
                Description = description.Trim(),
                Amount = amount,
                Category = category,
                ReceiptPath = receiptPath,
                LoggedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { ok = true, message = "Expense saved." });
        }
    }
}