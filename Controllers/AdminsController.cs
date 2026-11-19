using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;

namespace TMS.Controllers
{
    [AuthorizeRole("SuperAdmin")]
    public class AdminsController : Controller
    {
        private readonly TuitionProContext _db;
        public AdminsController(TuitionProContext db) { _db = db; }

        // GET: /Admins
        public async Task<IActionResult> Index()
        {
            var admins = await _db.Admins
                .Include(a => a.User)
                .OrderBy(a => a.User.FullName)
                .ToListAsync();
            return View(admins);
        }

        // GET: /Admins/Create
        public IActionResult Create() => View();

        // POST: /Admins/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string fullName, string email, string password)
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

            // Admins and Super Admins share the ADM- prefix
            var ids = await _db.Users
                .Where(u => u.Role == "Admin" || u.Role == "SuperAdmin")
                .Select(u => u.SystemId).ToListAsync();
            int next = ids.Select(id => int.TryParse(id.Substring(4), out var n) ? n : 0)
                          .DefaultIfEmpty(0).Max() + 1;

            var user = new TMS.Models.Domain.User
            {
                SystemId = SystemIdHelper.Generate("Admin", next),
                FullName = fullName.Trim(),
                Email = email,
                PasswordHash = PasswordHelper.Hash(password),
                Role = "Admin",
                Admin = new TMS.Models.Domain.Admin { AdminType = "Admin" }
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Administrator created. Login ID: {user.SystemId}";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admins/Toggle/5   (id = UserId)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var user = await _db.Users.FindAsync(id);

            // only ordinary Administrator accounts can be switched on or off
            if (user != null && user.Role == "Admin")
            {
                user.IsActive = !user.IsActive;
                await _db.SaveChangesAsync();
                TempData["Success"] = user.IsActive ? "Account activated." : "Account deactivated.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}