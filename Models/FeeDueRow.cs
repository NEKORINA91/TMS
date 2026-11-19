namespace TMS.Models
{
    public class FeeDueRow
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public string StudentName { get; set; } = "";
        public string SystemId { get; set; } = "";
        public string ClassName { get; set; } = "";
        public decimal AmountDue { get; set; }
    }
}