using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class KhoDuocController : Controller
{
    private readonly HtttbenhvienContext _context;

    public KhoDuocController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        int pageSize = 25;
        var query = _context.Medicines.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(m => m.MedicineName != null && m.MedicineName.ToLower().Contains(search)
                || (m.MedicineCode != null && m.MedicineCode.ToLower().Contains(search)));
        }

        ViewBag.Search = search;
        var total = await query.CountAsync();
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var medicines = await query
            .OrderBy(m => m.MedicineName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var lowStock = medicines.Count(m => m.StockQuantity < 100);
        var expiring = medicines.Count(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date <= DateTime.Now.Date.AddDays(30));
        ViewBag.LowStock = lowStock;
        ViewBag.Expiring = expiring;

        return View(medicines);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Medicine { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Medicine model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _context.Medicines.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Da them thuoc " + model.MedicineName;
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var med = await _context.Medicines.FindAsync(id);
        if (med == null) return NotFound();
        return View(med);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Medicine model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _context.Medicines.Update(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cap nhat thong tin thuoc thanh cong.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var med = await _context.Medicines.FindAsync(id);
        if (med == null) return NotFound();

        _context.Medicines.Remove(med);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Da xoa thuoc " + med.MedicineName;
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> BaoCaoTon()
    {
        var medicines = await _context.Medicines.OrderBy(m => m.MedicineName).ToListAsync();

        decimal totalValue = 0;
        foreach (var m in medicines)
        {
            totalValue += (m.Price) * m.StockQuantity;
        }

        ViewBag.TotalValue = totalValue;
        ViewBag.LowStockCount = medicines.Count(m => m.StockQuantity < 100);
        ViewBag.ExpiringCount = medicines.Count(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date <= DateTime.Now.Date.AddDays(30));

        return View(medicines);
    }
}
