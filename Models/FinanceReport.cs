namespace TMS.Models
{
    public class ReportLine
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
        public decimal Total { get; set; }
    }

    public class FinanceReport
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<ReportLine> Income { get; set; } = new();
        public List<ReportLine> Expenses { get; set; } = new();
        public decimal Payouts { get; set; }

        public decimal TotalIncome => Income.Sum(x => x.Total);
        public decimal TotalExpenses => Expenses.Sum(x => x.Total);
        public decimal Balance => TotalIncome - Payouts - TotalExpenses;
    }
}