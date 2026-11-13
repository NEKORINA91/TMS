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
    }
}