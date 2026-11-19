using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class ReportsController : Controller
    {
        private readonly TuitionProContext _db;
        public ReportsController(TuitionProContext db) { _db = db; }

        // default range = first of this month to today
        private static (DateTime from, DateTime to) Range(DateTime? from, DateTime? to)
        {
            var today = DateTime.Today;
            var f = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
            var t = (to ?? today).Date;
            if (t < f) t = f;
            return (f, t);
        }

        private async Task<FinanceReport> BuildReport(DateTime f, DateTime t)
        {
            var end = t.AddDays(1);

            var income = await _db.FeeCollections
                .Where(x => x.Status == "Paid" && x.PaidAt >= f && x.PaidAt < end)
                .GroupBy(x => x.Class.ClassName)
                .Select(g => new ReportLine { Label = g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            var expenses = await _db.Expenses
                .Where(x => x.LoggedAt >= f && x.LoggedAt < end)
                .GroupBy(x => x.Category)
                .Select(g => new ReportLine { Label = g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            decimal payouts = await _db.TeacherPayouts
                .Where(p => p.Status == "Paid" && p.ProcessedAt >= f && p.ProcessedAt < end)
                .SumAsync(p => (decimal?)p.NetPayout) ?? 0m;

            return new FinanceReport
            {
                From = f,
                To = t,
                Income = income.OrderBy(x => x.Label).ToList(),
                Expenses = expenses.OrderBy(x => x.Label).ToList(),
                Payouts = payouts
            };
        }

        // GET: /Reports?from=2026-11-01&to=2026-11-19
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var (f, t) = Range(from, to);
            return View(await BuildReport(f, t));
        }

        // GET: /Reports/ExportCsv?from=...&to=...
        public async Task<IActionResult> ExportCsv(DateTime? from, DateTime? to)
        {
            var (f, t) = Range(from, to);
            var r = await BuildReport(f, t);

            string N(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
            string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

            var sb = new StringBuilder();
            sb.AppendLine($"TMS Financial Report,{f:yyyy-MM-dd},{t:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine("Fee income by class");
            sb.AppendLine("Class,Payments,Total");
            foreach (var l in r.Income) sb.AppendLine($"{Q(l.Label)},{l.Count},{N(l.Total)}");
            sb.AppendLine();
            sb.AppendLine("Expenses by category");
            sb.AppendLine("Category,Entries,Total");
            foreach (var l in r.Expenses) sb.AppendLine($"{Q(l.Label)},{l.Count},{N(l.Total)}");
            sb.AppendLine();
            sb.AppendLine($"Total fee income,{N(r.TotalIncome)}");
            sb.AppendLine($"Teacher payouts paid,{N(r.Payouts)}");
            sb.AppendLine($"Total expenses,{N(r.TotalExpenses)}");
            sb.AppendLine($"Balance,{N(r.Balance)}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
                        $"report_{f:yyyyMMdd}_{t:yyyyMMdd}.csv");
        }
    }
}