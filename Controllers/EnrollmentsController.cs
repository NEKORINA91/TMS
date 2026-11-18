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
    }
}