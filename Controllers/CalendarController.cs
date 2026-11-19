using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models.Domain;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin", "Teacher", "Student")]
    public class CalendarController : Controller
    {
        private readonly TuitionProContext _db;
        public CalendarController(TuitionProContext db) { _db = db; }

        private static bool IsAdminRole(string? r) => r == "Admin" || r == "SuperAdmin";
        private static readonly string[] Kinds = { "Holiday", "Event", "Exam" };

        // GET: /Calendar
        public IActionResult Index()
        {
            ViewBag.Role = HttpContext.Session.GetSessionUser()?.Role;
            return View();
        }

        // GET: /Calendar/Data?month=2026-11   (called by the page)
        public async Task<IActionResult> Data(string? month)
        {
            var user = HttpContext.Session.GetSessionUser()!;
            if (!DateTime.TryParseExact((month ?? "") + "-01", "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var ms))
                ms = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var me = ms.AddMonths(1);

            var classes = await _db.Classes
                .Where(c => c.IsActive)
                .Include(c => c.Teacher).ThenInclude(t => t.User)
                .Include(c => c.Subject)
                .ToListAsync();

            // students only see the classes they are enrolled in
            if (user.Role == "Student")
            {
                var mine = await _db.Enrollments
                    .Where(e => e.StudentId == user.ProfileId && e.IsActive)
                    .Select(e => e.ClassId).ToListAsync();
                classes = classes.Where(c => mine.Contains(c.ClassId)).ToList();
            }

            var cancels = await _db.ClassCancellations
                .Where(x => x.ClassDate >= ms && x.ClassDate < me).ToListAsync();

            var items = new List<object>();
            for (var d = ms; d < me; d = d.AddDays(1))
            {
                string dayName = d.DayOfWeek.ToString();
                foreach (var c in classes.Where(c => c.DayOfWeek == dayName && d >= c.CreatedAt.Date))
                {
                    var can = cancels.FirstOrDefault(x => x.ClassId == c.ClassId && x.ClassDate.Date == d);
                    items.Add(new
                    {
                        date = d.ToString("yyyy-MM-dd"),
                        classId = c.ClassId,
                        name = c.ClassName,
                        subject = c.Subject.SubjectName,
                        teacher = c.Teacher.User.FullName,
                        room = c.Room,
                        start = c.StartTime.ToString(@"hh\:mm"),
                        end = c.EndTime.ToString(@"hh\:mm"),
                        mine = user.Role != "Teacher" || c.TeacherId == user.ProfileId,
                        cancelled = can != null,
                        reason = can?.Reason
                    });
                }
            }

            var events = (await _db.CalendarEvents
                .Where(e => e.EventDate >= ms && e.EventDate < me)
                .OrderBy(e => e.EventDate).ToListAsync())
                .Select(e => new { id = e.EventId, date = e.EventDate.ToString("yyyy-MM-dd"), title = e.Title, kind = e.Kind, notes = e.Notes })
                .ToList();

            return Json(new { items, events });
        }

        // POST: /Calendar/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int classId, DateTime date, string? reason)
        {
            var user = HttpContext.Session.GetSessionUser()!;
            var day = date.Date;

            if (user.Role != "Teacher" && !IsAdminRole(user.Role))
                return Json(new { ok = false, message = "You are not allowed to cancel classes." });
            if (day < DateTime.Today)
                return Json(new { ok = false, message = "Past classes cannot be cancelled." });

            var c = await _db.Classes.FirstOrDefaultAsync(x => x.ClassId == classId && x.IsActive);
            if (c == null || c.DayOfWeek != day.DayOfWeek.ToString())
                return Json(new { ok = false, message = "This class does not run on that day." });
            if (user.Role == "Teacher" && c.TeacherId != user.ProfileId)
                return Json(new { ok = false, message = "You can only cancel your own classes." });
            if (await _db.ClassCancellations.AnyAsync(x => x.ClassId == classId && x.ClassDate == day))
                return Json(new { ok = false, message = "This class is already cancelled." });

            _db.ClassCancellations.Add(new ClassCancellation
            {
                ClassId = classId,
                ClassDate = day,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                CancelledBy = user.SystemId
            });
            await _db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        // POST: /Calendar/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int classId, DateTime date)
        {
            var user = HttpContext.Session.GetSessionUser()!;
            var day = date.Date;

            if (user.Role != "Teacher" && !IsAdminRole(user.Role))
                return Json(new { ok = false, message = "You are not allowed to do that." });
            if (day < DateTime.Today)
                return Json(new { ok = false, message = "Past classes cannot be changed." });

            var c = await _db.Classes.FirstOrDefaultAsync(x => x.ClassId == classId);
            if (c == null) return Json(new { ok = false, message = "Class not found." });
            if (user.Role == "Teacher" && c.TeacherId != user.ProfileId)
                return Json(new { ok = false, message = "You can only change your own classes." });

            var rec = await _db.ClassCancellations.FirstOrDefaultAsync(x => x.ClassId == classId && x.ClassDate == day);
            if (rec != null)
            {
                _db.ClassCancellations.Remove(rec);
                await _db.SaveChangesAsync();
            }
            return Json(new { ok = true });
        }

        // POST: /Calendar/AddEvent   (administrators only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(string title, DateTime date, string kind, string? notes)
        {
            var user = HttpContext.Session.GetSessionUser()!;
            if (!IsAdminRole(user.Role))
                return Json(new { ok = false, message = "Only administrators can add events." });
            if (string.IsNullOrWhiteSpace(title) || !Kinds.Contains(kind))
                return Json(new { ok = false, message = "Enter a title and choose a type." });

            _db.CalendarEvents.Add(new CalendarEvent
            {
                Title = title.Trim(),
                EventDate = date.Date,
                Kind = kind,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                CreatedBy = user.SystemId
            });
            await _db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        // POST: /Calendar/DeleteEvent   (administrators only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var user = HttpContext.Session.GetSessionUser()!;
            if (!IsAdminRole(user.Role))
                return Json(new { ok = false, message = "Only administrators can delete events." });

            var e = await _db.CalendarEvents.FindAsync(id);
            if (e != null)
            {
                _db.CalendarEvents.Remove(e);
                await _db.SaveChangesAsync();
            }
            return Json(new { ok = true });
        }
    }
}