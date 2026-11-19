using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models;
using TMS.Models.Domain;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class PayoutsController : Controller
    {
        private readonly TuitionProContext _db;
        public PayoutsController(TuitionProContext db) { _db = db; }

        private static DateTime FirstOfMonth(DateTime d) => new DateTime(d.Year, d.Month, 1);

        // total PAID fees per teacher for the month
        private async Task<Dictionary<int, decimal>> GrossByTeacher(DateTime monthStart)
        {
            var monthEnd = monthStart.AddMonths(1);
            return await _db.FeeCollections
                .Where(f => f.Status == "Paid" && f.PeriodMonth >= monthStart && f.PeriodMonth < monthEnd)
                .GroupBy(f => f.Class.TeacherId)
                .Select(g => new { TeacherId = g.Key, Gross = g.Sum(f => f.Amount) })
                .ToDictionaryAsync(x => x.TeacherId, x => x.Gross);
        }

        // GET: /Payouts?month=2026-11
        public async Task<IActionResult> Index(DateTime? month)
        {
            var monthStart = FirstOfMonth(month ?? DateTime.Today);
            var gross = await GrossByTeacher(monthStart);
            var saved = await _db.TeacherPayouts
                .Where(p => p.PeriodMonth == monthStart)
                .ToDictionaryAsync(p => p.TeacherId);
            var teachers = await _db.Teachers.Include(t => t.User)
                .OrderBy(t => t.User.FullName).ToListAsync();

            var rows = new List<PayoutRow>();
            foreach (var t in teachers)
            {
                gross.TryGetValue(t.TeacherId, out var live);
                saved.TryGetValue(t.TeacherId, out var p);

                decimal deduction = p?.FacilityDeduction ?? Math.Round(live * t.CommissionRate / 100m, 2);
                rows.Add(new PayoutRow
                {
                    PayoutId = p?.PayoutId,
                    TeacherName = t.User.FullName,
                    SystemId = t.User.SystemId,
                    Rate = t.CommissionRate,
                    Gross = p?.GrossCollection ?? live,
                    Deduction = deduction,
                    Net = p?.NetPayout ?? (live - deduction),
                    Status = p?.Status
                });
            }

            ViewBag.Month = monthStart;
            return View(rows);
        }

        // POST: /Payouts/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(DateTime month)
        {
            var monthStart = FirstOfMonth(month);
            string by = HttpContext.Session.GetSessionUser()?.SystemId ?? "Admin";

            var teachers = await _db.Teachers.ToListAsync();
            var gross = await GrossByTeacher(monthStart);
            var existing = await _db.TeacherPayouts
                .Where(p => p.PeriodMonth == monthStart).ToListAsync();

            int created = 0;
            foreach (var t in teachers)
            {
                gross.TryGetValue(t.TeacherId, out var g);
                if (g <= 0) continue;

                decimal deduction = Math.Round(g * t.CommissionRate / 100m, 2);
                var rec = existing.FirstOrDefault(p => p.TeacherId == t.TeacherId);

                if (rec == null)
                {
                    _db.TeacherPayouts.Add(new TeacherPayout
                    {
                        TeacherId = t.TeacherId,
                        PeriodMonth = monthStart,
                        GrossCollection = g,
                        FacilityDeduction = deduction,
                        NetPayout = g - deduction,
                        Status = "Pending",
                        ProcessedBy = by
                    });
                    created++;
                }
                else if (rec.Status == "Pending")
                {
                    rec.GrossCollection = g;
                    rec.FacilityDeduction = deduction;
                    rec.NetPayout = g - deduction;
                }
                // a payout already marked Paid is never changed
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Payouts generated: {created} new.";
            return RedirectToAction(nameof(Index), new { month = monthStart.ToString("yyyy-MM") });
        }

        // POST: /Payouts/MarkPaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var rec = await _db.TeacherPayouts.FindAsync(id);
            if (rec != null && rec.Status != "Paid")
            {
                rec.Status = "Paid";
                rec.ProcessedBy = HttpContext.Session.GetSessionUser()?.SystemId ?? "Admin";
                rec.ProcessedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Payout marked as paid.";
            }
            var m = rec?.PeriodMonth ?? DateTime.Today;
            return RedirectToAction(nameof(Index), new { month = m.ToString("yyyy-MM") });
        }
    }
}