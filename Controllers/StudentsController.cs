using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class StudentsController : Controller
    {
        // GET: /Students/Create
        public IActionResult Create() => View();

        // POST: /Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string fullName, string email,
            string password, string guardianPhone, string address)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("", "Name, email and a password (min 6 characters) are required.");
                return View();
            }

            email = email.Trim().ToLower();
            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "That email is already registered.");
                return View();
            }

            // next ID: find the highest existing STD-### number and add 1
            var ids = await _db.Users.Where(u => u.Role == "Student")
                                     .Select(u => u.SystemId).ToListAsync();
            int next = ids.Select(id => int.TryParse(id.Substring(4), out var n) ? n : 0)
                          .DefaultIfEmpty(0).Max() + 1;

            var user = new TMS.Models.Domain.User
            {
                SystemId = SystemIdHelper.Generate("Student", next),
                FullName = fullName.Trim(),
                Email = email,
                PasswordHash = PasswordHelper.Hash(password),
                Role = "Student",
                Student = new TMS.Models.Domain.Student
                {
                    GuardianPhone = guardianPhone,
                    Address = address
                }
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Student created. Login ID: {user.SystemId}";
            return RedirectToAction(nameof(Index));
        }

        private readonly TuitionProContext _db;

        public StudentsController(TuitionProContext db)
        {
            _db = db;
        }

        // GET: /Students
        public async Task<IActionResult> Index()
        {
            var students = await _db.Students
                .Include(s => s.User)
                .OrderBy(s => s.User.FullName)
                .ToListAsync();

            return View(students);
        }

        // GET: /Students/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var s = await _db.Students.Include(x => x.User).FirstOrDefaultAsync(x => x.StudentId == id);
            if (s == null) return NotFound();
            return View(s);
        }

        // POST: /Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string fullName, string email,
            string guardianPhone, string address, bool isActive, string? newPassword)
        {
            var s = await _db.Students.Include(x => x.User).FirstOrDefaultAsync(x => x.StudentId == id);
            if (s == null) return NotFound();

            email = (email ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(fullName) || email == "")
            {
                ModelState.AddModelError("", "Name and email are required.");
                return View(s);
            }
            if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != s.UserId))
            {
                ModelState.AddModelError("", "That email belongs to another account.");
                return View(s);
            }
            if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < 6)
            {
                ModelState.AddModelError("", "A new password must be at least 6 characters.");
                return View(s);
            }

            s.User.FullName = fullName.Trim();
            s.User.Email = email;
            s.User.IsActive = isActive;
            if (!string.IsNullOrEmpty(newPassword))
                s.User.PasswordHash = PasswordHelper.Hash(newPassword);
            s.GuardianPhone = guardianPhone;
            s.Address = address;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Student updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Students/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.Students.Include(x => x.User).FirstOrDefaultAsync(x => x.StudentId == id);
            if (s == null) return NotFound();

            bool hasHistory = await _db.Enrollments.AnyAsync(e => e.StudentId == id)
                           || await _db.FeeCollections.AnyAsync(f => f.StudentId == id);

            if (hasHistory)
            {
                s.User.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = "This student has enrolment or fee records, so the account was deactivated instead of deleted.";
            }
            else
            {
                _db.Students.Remove(s);
                _db.Users.Remove(s.User);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Student deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}