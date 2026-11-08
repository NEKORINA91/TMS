using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Domain;
using TMS.DAL;
using TMS.Helpers;

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