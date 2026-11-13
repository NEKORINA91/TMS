using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using TMS.DAL;
using TMS.Helpers;

namespace TMS.Controllers
{
    public class AuthController : Controller
    {
        private readonly TuitionProContext _db;

        public AuthController(TuitionProContext db)
        {
            _db = db;
        }

        // GET /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.IsLoggedIn())
                return RedirectToDashboard(HttpContext.Session.GetSessionUser().Role);

            return View();
        }

        // POST /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string systemId, string password)
        {
            if (string.IsNullOrWhiteSpace(systemId) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter your System ID and password.";
                return View();
            }

            var user = _db.Users
                          .FirstOrDefault(u => u.SystemId == systemId && u.IsActive);

            if (user == null || !PasswordHelper.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid System ID or password.";
                return View();
            }

            int profileId = 0;
            if (user.Role == "SuperAdmin" || user.Role == "Admin")
                profileId = _db.Admins.Where(a => a.UserId == user.UserId).Select(a => a.AdminId).FirstOrDefault();
            else if (user.Role == "Teacher")
                profileId = _db.Teachers.Where(t => t.UserId == user.UserId).Select(t => t.TeacherId).FirstOrDefault();
            else if (user.Role == "Student")
                profileId = _db.Students.Where(s => s.UserId == user.UserId).Select(s => s.StudentId).FirstOrDefault();

            HttpContext.Session.SetSessionUser(new SessionUser
            {
                UserId    = user.UserId,
                SystemId  = user.SystemId,
                FullName  = user.FullName,
                Email     = user.Email,
                Role      = user.Role,
                ProfileId = profileId
            });

            user.LastLoginAt = DateTime.Now;
            _db.SaveChanges();

            return RedirectToDashboard(user.Role);
        }

        // POST /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.ClearSessionUser();
            return RedirectToAction("Login");
        }

        // GET /Auth/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToDashboard(string role)
        {
            if (role == "SuperAdmin" || role == "Admin")
                return RedirectToAction("Index", "AdminDashboard");
            if (role == "Teacher")    return RedirectToAction("Index", "TeacherDashboard");
            if (role == "Student")    return RedirectToAction("Index", "StudentDashboard");
            return RedirectToAction("Login");
        }
    }
}
