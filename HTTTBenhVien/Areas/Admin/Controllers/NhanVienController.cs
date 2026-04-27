using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class NhanVienController : Controller
{
    private readonly HtttbenhvienContext _context;

    public NhanVienController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        int pageSize = 25;
        var query = _context.Employees
            .Include(e => e.Dept)
            .Include(e => e.Account)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(e => e.FullName!.ToLower().Contains(search) || (e.Phone != null && e.Phone.Contains(search)));
        }

        ViewBag.Search = search;
        var total = await query.CountAsync();
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var employees = await query
            .OrderByDescending(e => e.EmpId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(employees);
    }

    [HttpGet]
    public IActionResult Create()
    {
        LoadDropdowns();
        return View(new Employee());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee model)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            return View(model);
        }

        _context.Employees.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm nhân viên {model.FullName}";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var emp = await _context.Employees.FindAsync(id);
        if (emp == null) return NotFound();
        LoadDropdowns();
        return View(emp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Employee model)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            return View(model);
        }

        _context.Employees.Update(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật nhân viên thành công.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var emp = await _context.Employees.FindAsync(id);
        if (emp == null) return NotFound();

        _context.Employees.Remove(emp);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa nhân viên {emp.FullName}";
        return RedirectToAction("Index");
    }

    private void LoadDropdowns()
    {
        ViewBag.Departments = _context.Departments.OrderBy(d => d.DeptName).ToList();
    }
}
