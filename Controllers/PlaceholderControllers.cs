using Microsoft.AspNetCore.Mvc;
using TMS.Helpers;

namespace TMS.Controllers
{
    public class AdminDashboardController : Controller
    {
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetSessionUser();
            if (user == null) return RedirectToAction("Login", "Auth");
            return Content($"Welcome {user.FullName}! Role: {user.Role} — Admin Dashboard coming soon.");
        }
    }

    public class SuperAdminController : Controller
    {
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetSessionUser();
            if (user == null) return RedirectToAction("Login", "Auth");
            return Content($"Welcome {user.FullName}! Role: {user.Role} — Super Admin coming soon.");
        }
    }

    public class TeacherDashboardController : Controller
    {
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetSessionUser();
            if (user == null) return RedirectToAction("Login", "Auth");
            return Content($"Welcome {user.FullName}! Role: {user.Role} — Teacher Dashboard coming soon.");
        }
    }

    public class StudentDashboardController : Controller
    {
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetSessionUser();
            if (user == null) return RedirectToAction("Login", "Auth");
            return Content($"Welcome {user.FullName}! Role: {user.Role} — Student Dashboard coming soon.");
        }
    }
}
