namespace TMS.Models
{
    public class PayoutRow
    {
        public int? PayoutId { get; set; }
        public string TeacherName { get; set; } = "";
        public string SystemId { get; set; } = "";
        public decimal Rate { get; set; }
        public decimal Gross { get; set; }
        public decimal Deduction { get; set; }
        public decimal Net { get; set; }
        public string? Status { get; set; }
    }
}