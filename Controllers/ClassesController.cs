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


        // GET: /Classes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Classes.FindAsync(id);
            if (c == null) return NotFound();
            await LoadLists();
            return View(c);
        }

        // POST: /Classes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int teacherId, int subjectId, string className,
            string dayOfWeek, string room, TimeSpan startTime, TimeSpan endTime,
            decimal monthlyFee, int maxCapacity, bool isActive)
        {
            var c = await _db.Classes.FindAsync(id);
            if (c == null) return NotFound();

            int enrolled = await _db.Enrollments.CountAsync(e => e.ClassId == id && e.IsActive);

            if (string.IsNullOrWhiteSpace(className) || monthlyFee < 0 || maxCapacity < 1
                || endTime <= startTime || maxCapacity < enrolled)
            {
                ModelState.AddModelError("", "Class name is required, fee cannot be negative, end time must be after start time, and capacity cannot be lower than the " + enrolled + " students already enrolled.");
                await LoadLists();
                return View(c);
            }

            c.TeacherId = teacherId;
            c.SubjectId = subjectId;
            c.ClassName = className.Trim();
            c.DayOfWeek = dayOfWeek;
            c.Room = room;
            c.StartTime = startTime;
            c.EndTime = endTime;
            c.MonthlyFee = monthlyFee;
            c.MaxCapacity = maxCapacity;
            c.IsActive = isActive;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Class updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Classes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Classes.FindAsync(id);
            if (c == null) return NotFound();

            bool hasHistory = await _db.Enrollments.AnyAsync(e => e.ClassId == id)
                           || await _db.FeeCollections.AnyAsync(f => f.ClassId == id);

            if (hasHistory)
            {
                c.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = "This class has enrolments or fee records, so it was marked inactive instead of deleted.";
            }
            else
            {
                _db.Classes.Remove(c);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Class deleted.";
            }
            return RedirectToAction(nameof(Index));
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