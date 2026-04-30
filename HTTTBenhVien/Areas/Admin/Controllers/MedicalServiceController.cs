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
    public class MedicalServiceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public MedicalServiceController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/MedicalService
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var services = _context.MedicalServices.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                services = services.Where(s =>
                    s.ServiceName.Contains(searchString) ||
                    (s.Description != null && s.Description.Contains(searchString)));
            }

            return View(await services.OrderBy(s => s.ServiceName).ToListAsync());
        }

        // GET: Admin/MedicalService/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.MedicalServices
                .Include(s => s.RecordServices)
                .FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null) return NotFound();

            return View(service);
        }

        // GET: Admin/MedicalService/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/MedicalService/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceId,ServiceName,BasePrice,Description")] MedicalService service)
        {
            ModelState.Remove("RecordServices");

            if (ModelState.IsValid)
            {
                _context.Add(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm dịch vụ y tế mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // GET: Admin/MedicalService/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.MedicalServices.FindAsync(id);
            if (service == null) return NotFound();

            return View(service);
        }

        // POST: Admin/MedicalService/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceId,ServiceName,BasePrice,Description")] MedicalService service)
        {
            if (id != service.ServiceId) return NotFound();

            ModelState.Remove("RecordServices");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật dịch vụ thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.ServiceId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // GET: Admin/MedicalService/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.MedicalServices
                .Include(s => s.RecordServices)
                .FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null) return NotFound();

            return View(service);
        }

        // POST: Admin/MedicalService/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.MedicalServices.FindAsync(id);
            if (service != null)
            {
                try
                {
                    _context.MedicalServices.Remove(service);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa dịch vụ thành công!";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Không thể xóa dịch vụ này vì đã được sử dụng trong bệnh án!";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.MedicalServices.Any(e => e.ServiceId == id);
        }
    }
}
