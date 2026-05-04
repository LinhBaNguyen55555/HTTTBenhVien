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
    public class MedicalSupplyController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public MedicalSupplyController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/MedicalSupply
        public async Task<IActionResult> Index(string searchString, string stockFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StockFilter"] = stockFilter;

            var supplies = _context.MedicalSupplies.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                supplies = supplies.Where(s =>
                    s.SupplyName.Contains(searchString) ||
                    (s.Supplier != null && s.Supplier.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(stockFilter))
            {
                switch (stockFilter)
                {
                    case "low":
                        supplies = supplies.Where(s => s.StockQuantity > 0 && s.StockQuantity <= 20);
                        break;
                    case "out":
                        supplies = supplies.Where(s => s.StockQuantity == 0);
                        break;
                    case "expired":
                        supplies = supplies.Where(s => s.ExpiryDate.HasValue && s.ExpiryDate < DateTime.Now);
                        break;
                }
            }

            return View(await supplies.OrderBy(s => s.SupplyName).ToListAsync());
        }

        // GET: Admin/MedicalSupply/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var supply = await _context.MedicalSupplies.FirstOrDefaultAsync(s => s.SupplyId == id);
            if (supply == null) return NotFound();
            return View(supply);
        }

        // GET: Admin/MedicalSupply/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/MedicalSupply/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SupplyId,SupplyName,Unit,StockQuantity,UnitPrice,ExpiryDate,Supplier,IsActive")] MedicalSupply supply)
        {
            if (ModelState.IsValid)
            {
                if (supply.IsActive == null) supply.IsActive = true;
                _context.Add(supply);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm vật tư mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(supply);
        }

        // GET: Admin/MedicalSupply/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var supply = await _context.MedicalSupplies.FindAsync(id);
            if (supply == null) return NotFound();
            return View(supply);
        }

        // POST: Admin/MedicalSupply/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SupplyId,SupplyName,Unit,StockQuantity,UnitPrice,ExpiryDate,Supplier,IsActive")] MedicalSupply supply)
        {
            if (id != supply.SupplyId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supply);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật vật tư thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.MedicalSupplies.Any(e => e.SupplyId == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supply);
        }

        // GET: Admin/MedicalSupply/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var supply = await _context.MedicalSupplies.FirstOrDefaultAsync(s => s.SupplyId == id);
            if (supply == null) return NotFound();
            return View(supply);
        }

        // POST: Admin/MedicalSupply/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supply = await _context.MedicalSupplies.FindAsync(id);
            if (supply != null)
            {
                _context.MedicalSupplies.Remove(supply);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa vật tư thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
