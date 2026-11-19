using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Models.Domain;

namespace TMS.Helpers
{
    public static class AttendanceHelper
    {
        // For every class that has already started today, give each enrolled student
        // with no record yet an "Absent" record. Scans and the switch change it to Present.
        public static async Task<int> EnsureAbsent(TuitionProContext db, DateTime now)
        {
            var today = now.Date;
            string dayName = now.DayOfWeek.ToString();
            var time = now.TimeOfDay;

            var missing = await db.Enrollments
                .Where(e => e.IsActive && e.Class.IsActive
                            && e.Class.DayOfWeek == dayName
                            && e.Class.StartTime <= time
                            && e.EnrolledDate <= today
                            && !e.AttendanceRecords.Any(a => a.ClassDate == today))
                .Select(e => e.EnrollmentId)
                .ToListAsync();

            foreach (var id in missing)
            {
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EnrollmentId = id,
                    ClassDate = today,
                    Status = "Absent",
                    MarkedBy = "System",
                    MarkedAt = now
                });
            }

            if (missing.Count > 0) await db.SaveChangesAsync();
            return missing.Count;
        }
    }
}