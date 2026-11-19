using System;

namespace TMS.Models
{
    public class TeacherClassRow
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string? DayOfWeek { get; set; }
        public string? Room { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Enrolled { get; set; }
    }

    public class StudentClassRow
    {
        public string ClassName { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string? DayOfWeek { get; set; }
        public string? Room { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
    }
}