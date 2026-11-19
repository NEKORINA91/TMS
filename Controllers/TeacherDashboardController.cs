using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models;

namespace TMS.Controllers
{
    [AuthorizeRole("Teacher")]
    public class TeacherDashboardController : Controller
    {
        private readonly TuitionProContext _db;
        public TeacherDashboardController(TuitionProContext db) { _db = db; }

        // GET: /TeacherDashboard
        public async Task<IActionResult> Index()
        {
            int teacherId = HttpContext.Session.GetSessionUser()?.ProfileId ?? 0;

            var classes = await _db.Classes
                .Where(c => c.TeacherId == teacherId && c.IsActive)
                .OrderBy(c => c.DayOfWeek).ThenBy(c => c.StartTime)
                .Select(c => new TeacherClassRow
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    SubjectName = c.Subject.SubjectName,
                    DayOfWeek = c.DayOfWeek,
                    Room = c.Room,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    Enrolled = c.Enrollments.Count(e => e.IsActive)
                }).ToListAsync();

            return View(classes);
        }

        // GET: /TeacherDashboard/Attendance?classId=1&date=2026-11-19  (read-only)
        public async Task<IActionResult> Attendance(int classId, DateTime? date)
        {
            int teacherId = HttpContext.Session.GetSessionUser()?.ProfileId ?? 0;

            // a teacher can only open their own classes
            var cls = await _db.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.TeacherId == teacherId);
            if (cls == null) return Redirect("/Auth/AccessDenied");

            var day = (date ?? DateTime.Today).Date;

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

            ViewBag.ClassName = cls.ClassName;
            ViewBag.ClassId = classId;
            ViewBag.Date = day;
            return View(rows);
        }
    }
}