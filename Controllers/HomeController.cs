using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TMS.Models;

namespace TMS.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(TMS.Helpers.PasswordHelper.Hash("Admin@123") + "<br/>");
            sb.Append(TMS.Helpers.PasswordHelper.Hash("Teacher@123") + "<br/>");
            sb.Append(TMS.Helpers.PasswordHelper.Hash("Student@123") + "<br/>");
            return Content(sb.ToString(), "text/html");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
