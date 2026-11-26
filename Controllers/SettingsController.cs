using Microsoft.AspNetCore.Mvc;
using TMS.Helpers;

namespace TMS.Controllers
{
    [AuthorizeRole("SuperAdmin")]
    public class SettingsController : Controller
    {
        private readonly InstituteSettings _inst;
        public SettingsController(InstituteSettings inst) { _inst = inst; }

        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string name, string? phone, string? email)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "The institute name is required.";
                return RedirectToAction(nameof(Index));
            }
            _inst.Save(name.Trim(), phone?.Trim() ?? "", email?.Trim() ?? "");
            TempData["Success"] = "Institute details saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}