using Microsoft.AspNetCore.Mvc;
using TMS.Helpers;

namespace TMS.Controllers
{
    public class SuperAdminController : Controller
    {
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetSessionUser();
            if (user == null) return RedirectToAction("Login", "Auth");
            return Content($"Welcome {user.FullName}! Role: {user.Role} — Super Admin coming soon.");
        }
    }
}