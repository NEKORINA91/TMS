using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models;
using TMS.Models.Domain;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class AttendanceController : Controller
    {
        private readonly TuitionProContext _db;
        public AttendanceController(TuitionProContext db) { _db = db; }

        // GET: /Attendance?classId=1&date=2026-11-18
        public async Task<IActionResult> Index(int? classId, DateTime? date)
        {
            var day = (date ?? DateTime.Today).Date;

            ViewBag.Classes = await _db.Classes
                .Where(c => c.IsActive)
                .OrderBy(c => c.ClassName)
                .Select(c => new { c.ClassId, c.ClassName }).ToListAsync();
            ViewBag.ClassId = classId;
            ViewBag.Date = day;

            if (classId == null)
                return View(new List<AttendanceRow>());

            var rows = await _db.Enrollments
                .Where(e => e.ClassId == classId && e.IsActive)
                .OrderBy(e => e.Student.User.FullName)
                .Select(e => new AttendanceRow
                {
                    EnrollmentId = e.EnrollmentId,
                    StudentName = e.Student.User.FullName,
                    SystemId = e.Student.User.SystemId,
                    Status = e.AttendanceRecords
                        .Where(a => a.ClassDate == day)
                        .Select(a => a.Status)
                        .FirstOrDefault()
                }).ToListAsync();

            return View(rows);
        }

        // POST: /Attendance/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int classId, DateTime date, Dictionary<int, string> status)
        {
            var day = date.Date;
            var back = new { classId, date = day.ToString("yyyy-MM-dd") };

            if (day > DateTime.Today)
            {
                TempData["Error"] = "Attendance cannot be logged for a future date.";
                return RedirectToAction(nameof(Index), back);
            }

            var enrollments = await _db.Enrollments
                .Where(e => e.ClassId == classId && e.IsActive).ToListAsync();

            var existing = await _db.AttendanceRecords
                .Where(a => a.Enrollment.ClassId == classId && a.ClassDate == day)
                .ToListAsync();

            var user = HttpContext.Session.GetSessionUser();
            string markedBy = user?.SystemId ?? "Admin";

            foreach (var enrollment in enrollments)
            {
                if (!status.TryGetValue(enrollment.EnrollmentId, out var value)
                    || (value != "Present" && value != "Absent"))
                    continue;

                var record = existing.FirstOrDefault(a => a.EnrollmentId == enrollment.EnrollmentId);
                if (record == null)
                {
                    _db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        ClassDate = day,
                        Status = value,
                        MarkedBy = markedBy,
                        MarkedAt = DateTime.Now
                    });
                }
                else
                {
                    record.Status = value;
                    record.MarkedBy = markedBy;
                    record.MarkedAt = DateTime.Now;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Attendance saved.";
            return RedirectToAction(nameof(Index), back);
        }
    }
}