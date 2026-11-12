using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class AdminDashboardController : Controller
    {
        private readonly TuitionProContext _db;

        public AdminDashboardController(TuitionProContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var nextMonth = monthStart.AddMonths(1);

            ViewBag.StudentCount = await _db.Students.CountAsync();
            ViewBag.TeacherCount = await _db.Teachers.CountAsync();
            ViewBag.ClassCount = await _db.Classes.CountAsync(c => c.IsActive);

            ViewBag.MonthRevenue = await _db.FeeCollections
                .Where(f => f.Status == "Paid" && f.PaidAt >= monthStart && f.PaidAt < nextMonth)
                .SumAsync(f => (decimal?)f.Amount) ?? 0m;

            ViewBag.MonthExpenses = await _db.Expenses
                .Where(e => e.LoggedAt >= monthStart && e.LoggedAt < nextMonth)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            ViewBag.PendingFees = await _db.FeeCollections.CountAsync(f => f.Status == "Pending");

            ViewBag.RecentFees = await _db.FeeCollections
                .Include(f => f.Student).ThenInclude(s => s.User)
                .Include(f => f.Class)
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}