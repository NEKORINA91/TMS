using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Domain;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class FeeCollectionsController : Controller
    {
        private readonly TuitionProContext _db;

        public FeeCollectionsController(TuitionProContext db)
        {
            _db = db;
        }

        // GET: /FeeCollections
        public async Task<IActionResult> Index()
        {
            var fees = await _db.FeeCollections
                .Include(f => f.Student).ThenInclude(s => s.User)
                .Include(f => f.Class)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(fees);
        }

        // GET: /FeeCollections/Due?month=2026-11
        public async Task<IActionResult> Due(DateTime? month)
        {
            var m = month ?? DateTime.Today;
            var monthStart = new DateTime(m.Year, m.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var rows = await _db.Enrollments
                .Where(e => e.IsActive && e.Class.IsActive && e.EnrolledDate < monthEnd)
                .Where(e => !e.Student.FeeCollections.Any(f =>
                        f.ClassId == e.ClassId
                        && f.Status == "Paid"
                        && f.PeriodMonth >= monthStart
                        && f.PeriodMonth < monthEnd))
                .OrderBy(e => e.Student.User.FullName)
                .Select(e => new FeeDueRow
                {
                    StudentId = e.StudentId,
                    ClassId = e.ClassId,
                    StudentName = e.Student.User.FullName,
                    SystemId = e.Student.User.SystemId,
                    ClassName = e.Class.ClassName,
                    AmountDue = e.Class.MonthlyFee
                }).ToListAsync();

            ViewBag.Month = monthStart;
            return View(rows);
        }

        // GET: /FeeCollections/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Students = await _db.Students
                .Include(s => s.User)
                .Select(s => new { s.StudentId, s.User.FullName })
                .ToListAsync();

            ViewBag.Classes = await _db.Classes
                .Select(c => new { c.ClassId, c.ClassName, c.MonthlyFee })
                .ToListAsync();

            return View();
        }

        // POST: /FeeCollections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int studentId, int classId, decimal amount, DateTime periodMonth)
        {
            if (amount <= 0)
            {
                ModelState.AddModelError("", "Amount must be greater than zero.");
                return await Create();
            }

            var user = HttpContext.Session.GetSessionUser();

            var fee = new FeeCollection
            {
                StudentId = studentId,
                ClassId = classId,
                Amount = amount,
                PeriodMonth = periodMonth,
                Status = "Paid",
                PaidAt = DateTime.Now,
                ReceiptNo = $"RC-{DateTime.Now:yyMMddHHmmss}",
                CollectedBy = user?.FullName ?? "Admin",
                CreatedAt = DateTime.Now
            };

            _db.FeeCollections.Add(fee);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Payment recorded. Receipt No: {fee.ReceiptNo}";
            return RedirectToAction(nameof(Index));
        }
    }
}