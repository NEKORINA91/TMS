using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models;

namespace TMS.Controllers
{
    [AuthorizeRole("Student")]
    public class StudentDashboardController : Controller
    {
        private readonly TuitionProContext _db;
        public StudentDashboardController(TuitionProContext db) { _db = db; }

        // GET: /StudentDashboard
        public async Task<IActionResult> Index()
        {
            int studentId = HttpContext.Session.GetSessionUser()?.ProfileId ?? 0;

            ViewBag.Classes = await _db.Enrollments
                .Where(e => e.StudentId == studentId && e.IsActive)
                .Select(e => new StudentClassRow
                {
                    ClassName = e.Class.ClassName,
                    SubjectName = e.Class.Subject.SubjectName,
                    DayOfWeek = e.Class.DayOfWeek,
                    Room = e.Class.Room,
                    StartTime = e.Class.StartTime,
                    EndTime = e.Class.EndTime,
                    Present = e.AttendanceRecords.Count(a => a.Status == "Present"),
                    Absent = e.AttendanceRecords.Count(a => a.Status == "Absent")
                }).ToListAsync();

            var fees = await _db.FeeCollections
                .Where(f => f.StudentId == studentId)
                .Include(f => f.Class)
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(fees);
        }
    }
}