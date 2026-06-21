using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class IcdDictionaryController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public IcdDictionaryController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/IcdDictionary
        public async Task<IActionResult> Index(string searchString, string chapterFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ChapterFilter"] = chapterFilter;


            ViewBag.Chapters = await _context.IcdDictionaries
                .Where(i => i.Chapter != null)
                .Select(i => i.Chapter)
                .Distinct()
                .ToListAsync();

            var icds = _context.IcdDictionaries.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                icds = icds.Where(i => 
                    i.IcdCode.Contains(searchString) || 
                    i.DiseaseName.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(chapterFilter))
            {
                icds = icds.Where(i => i.Chapter == chapterFilter);
            }

            return View(await icds.OrderBy(i => i.IcdCode).ToListAsync());
        }

        // GET: Admin/IcdDictionary/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/IcdDictionary/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IcdCode,DiseaseName,Chapter,IsActive")] IcdDictionary icdDictionary)
        {
            ModelState.Remove("MedicalRecords");

            if (ModelState.IsValid)
            {

                if (await _context.IcdDictionaries.AnyAsync(i => i.IcdCode == icdDictionary.IcdCode))
                {
                    ModelState.AddModelError("IcdCode", "Mã bệnh này đã tồn tại trong hệ thống.");
                    return View(icdDictionary);
                }

                if (icdDictionary.IsActive == null) icdDictionary.IsActive = true;

                _context.Add(icdDictionary);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mã bệnh mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(icdDictionary);
        }

        // GET: Admin/IcdDictionary/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var icdDictionary = await _context.IcdDictionaries.FindAsync(id);
            if (icdDictionary == null)
            {
                return NotFound();
            }
            return View(icdDictionary);
        }

        // POST: Admin/IcdDictionary/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("IcdCode,DiseaseName,Chapter,IsActive")] IcdDictionary icdDictionary)
        {
            if (id != icdDictionary.IcdCode)
            {
                return NotFound();
            }

            ModelState.Remove("MedicalRecords");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(icdDictionary);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật mã bệnh thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IcdDictionaryExists(icdDictionary.IcdCode))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(icdDictionary);
        }

        // POST: Admin/IcdDictionary/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var icdDictionary = await _context.IcdDictionaries.FindAsync(id);
            if (icdDictionary == null)
            {
                return NotFound();
            }

            icdDictionary.IsActive = !(icdDictionary.IsActive ?? true);
            _context.Update(icdDictionary);
            await _context.SaveChangesAsync();

            TempData["Success"] = icdDictionary.IsActive == true 
                ? $"Đã kích hoạt mã bệnh {icdDictionary.IcdCode}." 
                : $"Đã vô hiệu hóa mã bệnh {icdDictionary.IcdCode}.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/IcdDictionary/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var icdDictionary = await _context.IcdDictionaries
                .Include(i => i.MedicalRecords)
                .FirstOrDefaultAsync(m => m.IcdCode == id);

            if (icdDictionary == null)
            {
                return NotFound();
            }

            return View(icdDictionary);
        }

        // POST: Admin/IcdDictionary/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var icdDictionary = await _context.IcdDictionaries
                .Include(i => i.MedicalRecords)
                .FirstOrDefaultAsync(m => m.IcdCode == id);

            if (icdDictionary != null)
            {
                if (icdDictionary.MedicalRecords != null && icdDictionary.MedicalRecords.Any())
                {
                    TempData["Error"] = $"Không thể xóa mã bệnh {id} vì đã được sử dụng trong {icdDictionary.MedicalRecords.Count} bệnh án.";
                    return RedirectToAction(nameof(Index));
                }

                _context.IcdDictionaries.Remove(icdDictionary);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa mã bệnh thành công!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool IcdDictionaryExists(string id)
        {
            return _context.IcdDictionaries.Any(e => e.IcdCode == id);
        }
    }
}
