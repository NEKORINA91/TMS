using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models.Domain;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class EnrollmentsController : Controller
    {
        private readonly TuitionProContext _db;
        public EnrollmentsController(TuitionProContext db) { _db = db; }

        // GET: /Enrollments
        public async Task<IActionResult> Index()
        {
            var list = await _db.Enrollments
                .Where(e => e.IsActive)
                .Include(e => e.Student).ThenInclude(s => s.User)
                .Include(e => e.Class)
                .OrderBy(e => e.Class.ClassName)
                .ThenBy(e => e.Student.User.FullName)
                .ToListAsync();
            return View(list);
        }

        private async Task LoadLists()
        {
            ViewBag.Students = await _db.Students
                .Select(s => new { s.StudentId, s.User.FullName }).ToListAsync();

            ViewBag.Classes = await _db.Classes
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.ClassId,
                    c.ClassName,
                    c.MaxCapacity,
                    Enrolled = c.Enrollments.Count(e => e.IsActive)
                }).ToListAsync();
        }

        // GET: /Enrollments/Create
        public async Task<IActionResult> Create()
        {
            await LoadLists();
            return View();
        }

        // POST: /Enrollments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int studentId, int classId)
        {
            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId && c.IsActive);
            bool studentExists = await _db.Students.AnyAsync(s => s.StudentId == studentId);

            if (cls == null || !studentExists)
            {
                ModelState.AddModelError("", "Please choose a valid student and class.");
                await LoadLists();
                return View();
            }

            var existing = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.ClassId == classId);

            if (existing != null && existing.IsActive)
            {
                ModelState.AddModelError("", "This student is already enrolled in that class.");
                await LoadLists();
                return View();
            }

            int enrolled = await _db.Enrollments.CountAsync(e => e.ClassId == classId && e.IsActive);
            if (enrolled >= cls.MaxCapacity)
            {
                ModelState.AddModelError("", "This class is full.");
                await LoadLists();
                return View();
            }

            if (existing != null)
            {
                existing.IsActive = true;
                existing.EnrolledDate = DateTime.Today;
            }
            else
            {
                _db.Enrollments.Add(new Enrollment { StudentId = studentId, ClassId = classId });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Student enrolled.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Enrollments/Remove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var enrollment = await _db.Enrollments.FindAsync(id);
            if (enrollment != null)
            {
                enrollment.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Enrollment removed.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Enrollments/SearchStudents?q=kas
        public async Task<IActionResult> SearchStudents(string q)
        {
            q = (q ?? "").Trim();
            if (q.Length == 0) return Json(new object[0]);

            var list = await _db.Students
                .Where(s => s.User.FullName.Contains(q) || s.User.SystemId.Contains(q))
                .OrderBy(s => s.User.FullName)
                .Take(10)
                .Select(s => new { id = s.StudentId, text = s.User.SystemId + " - " + s.User.FullName })
                .ToListAsync();

            return Json(list);
        }

        private async Task LoadClassOptions()
        {
            ViewBag.Classes = await _db.Classes
                .Where(c => c.IsActive)
                .Select(c => new {
                    c.ClassId,
                    c.ClassName,
                    c.MaxCapacity,
                    Enrolled = c.Enrollments.Count(e => e.IsActive)
                })
                .ToListAsync();
        }

        // GET: /Enrollments/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var e = await _db.Enrollments
                .Include(x => x.Student).ThenInclude(s => s.User)
                .Include(x => x.Class)
                .FirstOrDefaultAsync(x => x.EnrollmentId == id);
            if (e == null) return NotFound();
            await LoadClassOptions();
            return View(e);
        }

        // POST: /Enrollments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int classId, bool isActive)
        {
            var e = await _db.Enrollments
                .Include(x => x.Student).ThenInclude(s => s.User)
                .Include(x => x.Class)
                .FirstOrDefaultAsync(x => x.EnrollmentId == id);
            if (e == null) return NotFound();

            if (classId != e.ClassId)
            {
                string? error = null;
                var target = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId && c.IsActive);

                if (target == null)
                    error = "Please choose a valid class.";
                else if (await _db.AttendanceRecords.AnyAsync(a => a.EnrollmentId == id))
                    error = "This enrolment already has attendance records, so it cannot be moved. Remove it and enrol the student again.";
                else if (await _db.Enrollments.AnyAsync(x => x.StudentId == e.StudentId && x.ClassId == classId))
                    error = "This student already has an enrolment in that class.";
                else if (await _db.Enrollments.CountAsync(x => x.ClassId == classId && x.IsActive) >= target.MaxCapacity)
                    error = "That class is full.";

                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    await LoadClassOptions();
                    return View(e);
                }
                e.ClassId = classId;
            }

            e.IsActive = isActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Enrollment updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Enrollments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.Enrollments.FindAsync(id);
            if (e == null) return NotFound();

            if (await _db.AttendanceRecords.AnyAsync(a => a.EnrollmentId == id))
            {
                e.IsActive = false;
                TempData["Success"] = "This enrolment has attendance records, so it was removed from the class but its history is kept.";
            }
            else
            {
                _db.Enrollments.Remove(e);
                TempData["Success"] = "Enrollment deleted.";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}