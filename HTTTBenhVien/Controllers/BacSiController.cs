using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers;

public class BacSiController : Controller
{
    private readonly HtttbenhvienContext _context;

    public BacSiController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var doctors = await _context.Employees
            .Include(e => e.Dept)
            .Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ"))
            .OrderBy(e => e.Dept!.DeptName)
            .ThenBy(e => e.FullName)
            .ToListAsync();

        return View(doctors);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var doctor = await _context.Employees
            .Include(e => e.Dept)
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e => e.EmpId == id);

        if (doctor == null)
            return NotFound();

        return View(doctor);
    }
}
