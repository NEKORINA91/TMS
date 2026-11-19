using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class TeachersController : Controller
    {
        private readonly TuitionProContext _db;
        public TeachersController(TuitionProContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var teachers = await _db.Teachers
                .Include(t => t.User)
                .OrderBy(t => t.User.FullName)
                .ToListAsync();
            return View(teachers);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string fullName, string email, string password,
            string qualifications, string shortBio, string contactNumber,
            decimal commissionRate, string payoutFrequency, string bankAccount)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(password) || password.Length < 6
                || commissionRate < 0 || commissionRate > 100)
            {
                ModelState.AddModelError("", "Name, email, password (min 6) and a commission between 0 and 100 are required.");
                return View();
            }

            email = email.Trim().ToLower();
            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "That email is already registered.");
                return View();
            }

            var ids = await _db.Users.Where(u => u.Role == "Teacher")
                                     .Select(u => u.SystemId).ToListAsync();
            int next = ids.Select(id => int.TryParse(id.Substring(4), out var n) ? n : 0)
                          .DefaultIfEmpty(0).Max() + 1;

            var user = new TMS.Models.Domain.User
            {
                SystemId = SystemIdHelper.Generate("Teacher", next),
                FullName = fullName.Trim(),
                Email = email,
                PasswordHash = PasswordHelper.Hash(password),
                Role = "Teacher",
                Teacher = new TMS.Models.Domain.Teacher
                {
                    Qualifications = qualifications,
                    ShortBio = shortBio,
                    ContactNumber = contactNumber,
                    CommissionRate = commissionRate,
                    PayoutFrequency = payoutFrequency ?? "Monthly",
                    BankAccount = bankAccount
                }
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Teacher created. Login ID: {user.SystemId}";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Teachers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var t = await _db.Teachers.Include(x => x.User).FirstOrDefaultAsync(x => x.TeacherId == id);
            if (t == null) return NotFound();
            return View(t);
        }

        // POST: /Teachers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string fullName, string email, string qualifications,
            string shortBio, string contactNumber, decimal commissionRate, string payoutFrequency,
            string bankAccount, bool isActive, string? newPassword)
        {
            var t = await _db.Teachers.Include(x => x.User).FirstOrDefaultAsync(x => x.TeacherId == id);
            if (t == null) return NotFound();

            email = (email ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(fullName) || email == "" || commissionRate < 0 || commissionRate > 100)
            {
                ModelState.AddModelError("", "Name, email and a commission between 0 and 100 are required.");
                return View(t);
            }
            if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != t.UserId))
            {
                ModelState.AddModelError("", "That email belongs to another account.");
                return View(t);
            }
            if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < 6)
            {
                ModelState.AddModelError("", "A new password must be at least 6 characters.");
                return View(t);
            }

            t.User.FullName = fullName.Trim();
            t.User.Email = email;
            t.User.IsActive = isActive;
            if (!string.IsNullOrEmpty(newPassword))
                t.User.PasswordHash = PasswordHelper.Hash(newPassword);
            t.Qualifications = qualifications;
            t.ShortBio = shortBio;
            t.ContactNumber = contactNumber;
            t.CommissionRate = commissionRate;
            t.PayoutFrequency = payoutFrequency ?? "Monthly";
            t.BankAccount = bankAccount;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Teacher updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Teachers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _db.Teachers.Include(x => x.User).FirstOrDefaultAsync(x => x.TeacherId == id);
            if (t == null) return NotFound();

            bool hasHistory = await _db.Classes.AnyAsync(c => c.TeacherId == id)
                           || await _db.TeacherPayouts.AnyAsync(p => p.TeacherId == id);

            if (hasHistory)
            {
                t.User.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = "This teacher has classes or payouts, so the account was deactivated instead of deleted.";
            }
            else
            {
                _db.Teachers.Remove(t);
                _db.Users.Remove(t.User);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Teacher deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}