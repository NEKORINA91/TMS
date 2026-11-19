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

        // GET: /Attendance?classId=1&date=2026-11-19
        public async Task<IActionResult> Index(int? classId, DateTime? date)
        {
            var now = DateTime.Now;
            await AttendanceHelper.EnsureAbsent(_db, now);   // catch up if the service was not running

            var day = (date ?? DateTime.Today).Date;

            ViewBag.Classes = await _db.Classes
                .Where(c => c.IsActive)
                .OrderBy(c => c.ClassName)
                .Select(c => new { c.ClassId, c.ClassName }).ToListAsync();

            // classes running now (from 30 minutes before the start until the end)
            string dayName = now.DayOfWeek.ToString();
            var early = TimeSpan.FromMinutes(30);
            var todays = await _db.Classes
                .Where(c => c.IsActive && c.DayOfWeek == dayName)
                .Select(c => new { c.ClassId, c.ClassName, c.StartTime, c.EndTime })
                .ToListAsync();
            ViewBag.Running = todays
                .Where(c => now.TimeOfDay >= c.StartTime - early && now.TimeOfDay <= c.EndTime)
                .ToList();

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

        // POST: /Attendance/SetStatus  (called when a switch is flipped)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int enrollmentId, DateTime date, bool present)
        {
            var day = date.Date;
            if (day > DateTime.Today)
                return Json(new { ok = false, message = "Future dates are not allowed." });

            var enrollment = await _db.Enrollments.FindAsync(enrollmentId);
            if (enrollment == null || !enrollment.IsActive)
                return Json(new { ok = false, message = "That student is not enrolled." });

            var record = await _db.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EnrollmentId == enrollmentId && a.ClassDate == day);

            string status = present ? "Present" : "Absent";
            string markedBy = HttpContext.Session.GetSessionUser()?.SystemId ?? "Admin";

            if (record == null)
            {
                _db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EnrollmentId = enrollmentId,
                    ClassDate = day,
                    Status = status,
                    MarkedBy = markedBy,
                    MarkedAt = DateTime.Now
                });
            }
            else
            {
                record.Status = status;
                record.MarkedBy = markedBy;
                record.MarkedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        // GET: /Attendance/Scan
        public IActionResult Scan() => View();

        // POST: /Attendance/ScanMark  (called by the scan page)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanMark(string code)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var time = now.TimeOfDay;
            string dayName = now.DayOfWeek.ToString();          // e.g. "Thursday"

            // the QR text is "STD-001|Student Name"; only the ID is used
            string systemId = (code ?? "").Split('|')[0].Trim();

            var student = await _db.Students.Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.SystemId == systemId && s.User.IsActive);
            if (student == null)
                return Json(new { ok = false, message = $"{systemId} was not found." });

            // this student's classes that run today
            var todays = await _db.Enrollments
                .Where(e => e.StudentId == student.StudentId && e.IsActive
                            && e.Class.IsActive && e.Class.DayOfWeek == dayName)
                .Select(e => new { e.EnrollmentId, e.Class.ClassName, e.Class.StartTime, e.Class.EndTime })
                .ToListAsync();

            // a scan counts for a class from 30 minutes before it starts until it ends
            var early = TimeSpan.FromMinutes(30);
            var match = todays
                .Where(c => time >= c.StartTime - early && time <= c.EndTime)
                .OrderBy(c => c.StartTime)
                .FirstOrDefault();

            if (match == null)
            {
                string note = todays.Any()
                    ? "Today: " + string.Join(", ", todays.Select(c => $"{c.ClassName} {c.StartTime:hh\\:mm}-{c.EndTime:hh\\:mm}"))
                    : "No class today.";
                return Json(new { ok = false, message = $"{student.User.FullName} has no class right now. {note}" });
            }

            var record = await _db.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EnrollmentId == match.EnrollmentId && a.ClassDate == today);
            string markedBy = HttpContext.Session.GetSessionUser()?.SystemId ?? "Admin";

            if (record == null)
            {
                _db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EnrollmentId = match.EnrollmentId,
                    ClassDate = today,
                    Status = "Present",
                    MarkedBy = markedBy,
                    MarkedAt = now
                });
            }
            else
            {
                record.Status = "Present";
                record.MarkedBy = markedBy;
                record.MarkedAt = now;
            }

            await _db.SaveChangesAsync();
            return Json(new
            {
                ok = true,
                name = student.User.FullName,
                message = $"marked Present in {match.ClassName} at {now:HH:mm}"
            });
        }

        // POST: /Attendance/ScanMark  (called by the scan page)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanMark(int classId, DateTime date, string code)
        {
            var day = date.Date;
            if (day > DateTime.Today)
                return Json(new { ok = false, message = "Future dates are not allowed." });

            // the QR text is "STD-001|Student Name", so only the part before | is used
            string systemId = (code ?? "").Split('|')[0].Trim();

            var enrollment = await _db.Enrollments
                .Include(e => e.Student).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(e => e.ClassId == classId && e.IsActive
                                          && e.Student.User.SystemId == systemId);

            if (enrollment == null)
                return Json(new { ok = false, message = $"{systemId} is not enrolled in this class." });

            var record = await _db.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EnrollmentId == enrollment.EnrollmentId && a.ClassDate == day);

            string markedBy = HttpContext.Session.GetSessionUser()?.SystemId ?? "Admin";

            if (record == null)
            {
                _db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    ClassDate = day,
                    Status = "Present",
                    MarkedBy = markedBy,
                    MarkedAt = DateTime.Now
                });
            }
            else
            {
                record.Status = "Present";
                record.MarkedBy = markedBy;
                record.MarkedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            return Json(new { ok = true, name = enrollment.Student.User.FullName, message = "marked Present" });
        }
    }
}