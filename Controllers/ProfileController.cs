using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;
using TMS.Models.Domain;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin", "Teacher", "Student")]
    public class ProfileController : Controller
    {
        private readonly TuitionProContext _db;
        private readonly IWebHostEnvironment _env;
        private static readonly string[] PhotoExt = { ".jpg", ".jpeg", ".png" };
        private const long MaxPhotoBytes = 2 * 1024 * 1024;   // 2 MB

        public ProfileController(TuitionProContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private async Task<User?> Me()
        {
            int id = HttpContext.Session.GetSessionUser()!.UserId;
            return await _db.Users
                .Include(u => u.Teacher)
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        private void DeletePhotoFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var full = Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var u = await Me();
            if (u == null) return NotFound();
            return View(u);
        }

        // POST: /Profile/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(string fullName, string email, string? contactNumber,
            string? qualifications, string? shortBio, string? bankAccount,
            string? guardianPhone, string? address, IFormFile? photo, bool removePhoto)
        {
            var u = await Me();
            if (u == null) return NotFound();

            email = (email ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(fullName) || email == "")
            {
                ModelState.AddModelError("", "Name and email are required.");
                return View("Index", u);
            }
            if (await _db.Users.AnyAsync(x => x.Email == email && x.UserId != u.UserId))
            {
                ModelState.AddModelError("", "That email belongs to another account.");
                return View("Index", u);
            }

            string? newPhoto = null;
            if (photo != null && photo.Length > 0)
            {
                var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
                if (!PhotoExt.Contains(ext) || photo.Length > MaxPhotoBytes)
                {
                    ModelState.AddModelError("", "The photo must be a JPG or PNG up to 2 MB.");
                    return View("Index", u);
                }
                var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(folder);
                var name = $"{Guid.NewGuid()}{ext}";
                using (var fs = new FileStream(Path.Combine(folder, name), FileMode.Create))
                    await photo.CopyToAsync(fs);
                newPhoto = $"/uploads/avatars/{name}";
            }

            u.FullName = fullName.Trim();
            u.Email = email;

            if (newPhoto != null)
            {
                DeletePhotoFile(u.ProfilePhoto);
                u.ProfilePhoto = newPhoto;
            }
            else if (removePhoto)
            {
                DeletePhotoFile(u.ProfilePhoto);
                u.ProfilePhoto = null;
            }

            if (u.Teacher != null)
            {
                u.Teacher.ContactNumber = contactNumber;
                u.Teacher.Qualifications = qualifications;
                u.Teacher.ShortBio = shortBio;
                u.Teacher.BankAccount = bankAccount;
            }
            if (u.Student != null)
            {
                u.Student.GuardianPhone = guardianPhone;
                u.Student.Address = address;
            }

            await _db.SaveChangesAsync();

            // refresh the name and photo shown in the top bar
            var s = HttpContext.Session.GetSessionUser()!;
            s.FullName = u.FullName;
            s.Email = u.Email;
            s.PhotoPath = u.ProfilePhoto;
            HttpContext.Session.SetSessionUser(s);

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var u = await Me();
            if (u == null) return NotFound();

            if (!PasswordHelper.Verify(currentPassword ?? "", u.PasswordHash))
                TempData["Error"] = "The current password is not correct.";
            else if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                TempData["Error"] = "The new password must be at least 6 characters.";
            else if (newPassword != confirmPassword)
                TempData["Error"] = "The new passwords do not match.";
            else
            {
                u.PasswordHash = PasswordHelper.Hash(newPassword);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Password changed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}