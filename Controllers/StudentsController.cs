using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.DAL;
using TMS.Helpers;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class StudentsController : Controller
    {
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
    }
}