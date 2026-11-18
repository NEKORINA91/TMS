namespace TMS.Models
{
    public class AttendanceRow
    {
        public int EnrollmentId { get; set; }
        public string StudentName { get; set; } = "";
        public string SystemId { get; set; } = "";
        public string? Status { get; set; }
    }
}