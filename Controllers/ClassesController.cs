using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class ClassesController : Controller
    {
        private readonly TuitionProContext _db;
        public ClassesController(TuitionProContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var classes = await _db.Classes
                .Include(c => c.Teacher).ThenInclude(t => t.User)
                .Include(c => c.Subject)
                .Include(c => c.Enrollments)
                .OrderBy(c => c.ClassName)
                .ToListAsync();
            return View(classes);
        }

        private async Task LoadLists()
        {
            ViewBag.Teachers = await _db.Teachers
                .Select(t => new { t.TeacherId, t.User.FullName }).ToListAsync();
            ViewBag.Subjects = await _db.Subjects
                .Select(s => new { s.SubjectId, s.SubjectName, s.Level }).ToListAsync();
        }

        public async Task<IActionResult> Create()
        {
            await LoadLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int teacherId, int subjectId, string className,
            string dayOfWeek, string room, TimeSpan startTime, TimeSpan endTime,
            decimal monthlyFee, int maxCapacity)
        {
            if (string.IsNullOrWhiteSpace(className) || monthlyFee < 0 || maxCapacity < 1
                || endTime <= startTime)
            {
                ModelState.AddModelError("", "Class name is required, fee cannot be negative, capacity must be at least 1, and end time must be after start time.");
                await LoadLists();
                return View();
            }

            var cls = new TMS.Models.Domain.Class
            {
                TeacherId = teacherId,
                SubjectId = subjectId,
                ClassName = className.Trim(),
                DayOfWeek = dayOfWeek,
                Room = room,
                StartTime = startTime,
                EndTime = endTime,
                MonthlyFee = monthlyFee,
                MaxCapacity = maxCapacity,
                IsActive = true
            };

            _db.Classes.Add(cls);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Class created.";
            return RedirectToAction(nameof(Index));
        }
    }
}