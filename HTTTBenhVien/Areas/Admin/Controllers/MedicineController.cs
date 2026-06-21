using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MedicineController : Controller
    {
        private readonly HtttbenhvienContext _context;
        private readonly IWebHostEnvironment _environment;

        public MedicineController(HtttbenhvienContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Admin/Medicine
        public async Task<IActionResult> Index(string searchString, string stockFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StockFilter"] = stockFilter;

            var medicines = _context.Medicines.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                medicines = medicines.Where(m =>
                    m.MedicineName.Contains(searchString) ||
                    (m.MedicineCode != null && m.MedicineCode.Contains(searchString)) ||
                    (m.ActiveIngredient != null && m.ActiveIngredient.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(stockFilter))
            {
                switch (stockFilter)
                {
                    case "low":
                        medicines = medicines.Where(m => m.StockQuantity > 0 && m.StockQuantity <= 20);
                        break;
                    case "out":
                        medicines = medicines.Where(m => m.StockQuantity == 0);
                        break;
                    case "expired":
                        medicines = medicines.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate < DateTime.Now);
                        break;
                }
            }

            return View(await medicines.OrderBy(m => m.MedicineName).ToListAsync());
        }

        // GET: Admin/Medicine/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var medicine = await _context.Medicines
                .Include(m => m.PrescriptionDetails)
                .FirstOrDefaultAsync(m => m.MedicineId == id);

            if (medicine == null) return NotFound();

            return View(medicine);
        }

        // GET: Admin/Medicine/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Medicine/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MedicineId,MedicineCode,MedicineName,ActiveIngredient,Unit,Price,StockQuantity,ExpiryDate,Description,IsActive")] Medicine medicine, IFormFile? ImageFile)
        {
            ModelState.Remove("PrescriptionDetails");
            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "img", "medicine");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }
                    medicine.ImageUrl = "/assets/img/medicine/" + uniqueFileName;
                }

                if (medicine.IsActive == null) medicine.IsActive = true;

                _context.Add(medicine);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm thuốc mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(medicine);
        }

        // GET: Admin/Medicine/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        // POST: Admin/Medicine/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MedicineId,MedicineCode,MedicineName,ActiveIngredient,Unit,Price,StockQuantity,ExpiryDate,ImageUrl,Description,IsActive")] Medicine medicine, IFormFile? ImageFile)
        {
            if (id != medicine.MedicineId) return NotFound();

            ModelState.Remove("PrescriptionDetails");
            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "img", "medicine");
                        Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(fileStream);
                        }
                        medicine.ImageUrl = "/assets/img/medicine/" + uniqueFileName;
                    }

                    _context.Update(medicine);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thuốc thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicineExists(medicine.MedicineId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["Error"] = "Cập nhật thất bại: " + string.Join(", ", errors);
            return View(medicine);
        }

        // GET: Admin/Medicine/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var medicine = await _context.Medicines
                .Include(m => m.PrescriptionDetails)
                .FirstOrDefaultAsync(m => m.MedicineId == id);

            if (medicine == null) return NotFound();
            return View(medicine);
        }

        // POST: Admin/Medicine/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine != null)
            {
                try
                {
                    _context.Medicines.Remove(medicine);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa thuốc thành công!";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Không thể xóa thuốc này vì đã được sử dụng trong đơn thuốc!";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MedicineExists(int id)
        {
            return _context.Medicines.Any(e => e.MedicineId == id);
        }
    }
}
