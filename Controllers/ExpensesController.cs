using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Domain;
using TMS.DAL;
using TMS.Helpers;

namespace TMS.Controllers
{
    [AuthorizeRole("Admin", "SuperAdmin")]
    public class ExpensesController : Controller
    {
        private readonly TuitionProContext _db;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] Categories =
            { "Utilities", "Maintenance", "Stationery", "Marketing", "Other" };

        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxReceiptBytes = 5 * 1024 * 1024; // 5 MB

        public ExpensesController(TuitionProContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: /Expenses
        public async Task<IActionResult> Index()
        {
            var expenses = await _db.Expenses
                .Include(e => e.Admin).ThenInclude(a => a.User)
                .OrderByDescending(e => e.LoggedAt)
                .ToListAsync();

            return View(expenses);
        }

        // GET: /Expenses/Create
        public IActionResult Create()
        {
            ViewBag.Categories = Categories;
            return View();
        }

        // POST: /Expenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string description, decimal amount, string category, IFormFile? receipt)
        {
            ViewBag.Categories = Categories;

            if (string.IsNullOrWhiteSpace(description) || amount <= 0 || !Categories.Contains(category))
            {
                ViewBag.Error = "Please enter a description, a positive amount, and a valid category.";
                return View();
            }

            string? receiptPath = null;

            if (receipt != null && receipt.Length > 0)
            {
                var ext = Path.GetExtension(receipt.FileName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(ext) || receipt.Length > MaxReceiptBytes)
                {
                    ViewBag.Error = "Receipt must be a PDF, JPG, or PNG file up to 5 MB.";
                    return View();
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await receipt.CopyToAsync(stream);
                }

                receiptPath = $"/uploads/receipts/{fileName}";
            }

            var user = HttpContext.Session.GetSessionUser();

            var expense = new Expense
            {
                AdminId = user!.ProfileId,   // set to AdminId at login
                Description = description.Trim(),
                Amount = amount,
                Category = category,
                ReceiptPath = receiptPath,
                LoggedAt = DateTime.Now
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Expense logged.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Expenses/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var e = await _db.Expenses.FindAsync(id);
            if (e == null) return NotFound();
            ViewBag.Categories = Categories;
            return View(e);
        }

        // POST: /Expenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string description, decimal amount,
            string category, IFormFile? receipt, bool removeReceipt)
        {
            var e = await _db.Expenses.FindAsync(id);
            if (e == null) return NotFound();
            ViewBag.Categories = Categories;

            if (string.IsNullOrWhiteSpace(description) || amount <= 0 || !Categories.Contains(category))
            {
                ViewBag.Error = "Please enter a description, a positive amount, and a valid category.";
                return View(e);
            }

            if (receipt != null && receipt.Length > 0)
            {
                var ext = Path.GetExtension(receipt.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext) || receipt.Length > MaxReceiptBytes)
                {
                    ViewBag.Error = "Receipt must be a PDF, JPG, or PNG file up to 5 MB.";
                    return View(e);
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid()}{ext}";
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                    await receipt.CopyToAsync(stream);

                DeleteReceiptFile(e.ReceiptPath);
                e.ReceiptPath = $"/uploads/receipts/{fileName}";
            }
            else if (removeReceipt)
            {
                DeleteReceiptFile(e.ReceiptPath);
                e.ReceiptPath = null;
            }

            e.Description = description.Trim();
            e.Amount = amount;
            e.Category = category;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Expense updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Expenses/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.Expenses.FindAsync(id);
            if (e == null) return NotFound();

            DeleteReceiptFile(e.ReceiptPath);
            _db.Expenses.Remove(e);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Expense deleted.";
            return RedirectToAction(nameof(Index));
        }

        private void DeleteReceiptFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var full = Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
    }
}