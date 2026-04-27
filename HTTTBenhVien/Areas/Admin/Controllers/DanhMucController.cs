using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DanhMucController : Controller
{
    private readonly HtttbenhvienContext _context;

    public DanhMucController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        int pageSize = 30;
        var query = _context.IcdDictionaries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = "%" + search.Trim() + "%";
            query = query.Where(i => EF.Functions.Like(i.IcdCode, s)
                || EF.Functions.Like(i.DiseaseName, s));
        }

        ViewBag.Search = search;
        var total = await query.CountAsync();
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var icds = await query.OrderBy(i => i.IcdCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View(icds);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new IcdDictionary { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IcdDictionary model)
    {
        if (!ModelState.IsValid) return View(model);
        _context.IcdDictionaries.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Da them ICD " + model.IcdCode;
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var icd = await _context.IcdDictionaries.FindAsync(id);
        if (icd == null) return NotFound();
        return View(icd);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(IcdDictionary model)
    {
        if (!ModelState.IsValid) return View(model);
        _context.IcdDictionaries.Update(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cap nhat ICD thanh cong.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(string id)
    {
        var icd = await _context.IcdDictionaries.FindAsync(id);
        if (icd != null)
        {
            _context.IcdDictionaries.Remove(icd);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Da xoa ICD.";
        }
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Search(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Json(new { results = Array.Empty<object>() });

        var k = keyword.Trim();
        var icds = await _context.IcdDictionaries
            .Where(i => i.IsActive == true)
            .Where(i => EF.Functions.Like(i.IcdCode, "%" + k + "%")
                || EF.Functions.Like(i.DiseaseName, "%" + k + "%"))
            .Take(20)
            .ToListAsync();

        var results = icds.Select(i => new { id = i.IcdCode, text = i.IcdCode + " - " + i.DiseaseName }).ToList();
        return Json(new { results });
    }
}
