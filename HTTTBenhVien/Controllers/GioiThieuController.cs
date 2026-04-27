using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers;

public class GioiThieuController : Controller
{
    private readonly HtttbenhvienContext _context;

    public GioiThieuController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _context.Employees
            .Include(e => e.Dept)
            .Where(e => e.JobTitle != null && (e.JobTitle.Contains("Giám đốc") || e.JobTitle.Contains("Phó giám đốc") || e.JobTitle.Contains("Trưởng khoa")))
            .Take(6)
            .ToListAsync();

        var departments = await _context.Departments
            .Where(d => d.DeptType == "Lâm sàng")
            .OrderBy(d => d.DeptName)
            .Take(8)
            .ToListAsync();

        ViewBag.Leadership = employees;
        ViewBag.Departments = departments;

        return View();
    }

    public IActionResult LichSu()
    {
        return View();
    }

    public IActionResult CoSoVatChat()
    {
        return View();
    }

    public IActionResult TamNhinSuMenh()
    {
        return View();
    }
}
