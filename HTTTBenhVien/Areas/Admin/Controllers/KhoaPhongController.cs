using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class KhoaPhongController : Controller
{
    private readonly HtttbenhvienContext _context;

    public KhoaPhongController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var depts = await _context.Departments
            .Include(d => d.Employees)
            .OrderBy(d => d.DeptType)
            .ThenBy(d => d.DeptName)
            .ToListAsync();
        return View(depts);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Department());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department model)
    {
        if (!ModelState.IsValid) return View(model);
        _context.Departments.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm khoa {model.DeptName}";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        return View(dept);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Department model)
    {
        if (!ModelState.IsValid) return View(model);
        _context.Departments.Update(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật khoa thành công.";
        return RedirectToAction("Index");
    }
}
