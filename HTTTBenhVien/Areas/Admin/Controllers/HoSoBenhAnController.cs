using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class HoSoBenhAnController : Controller
{
    private readonly HtttbenhvienContext _context;

    public HoSoBenhAnController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        int pageSize = 25;
        var query = _context.MedicalRecords
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .ThenInclude(d => d!.Dept)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(m => m.Patient!.FullName!.ToLower().Contains(search));
        }

        ViewBag.Search = search;
        var total = await query.CountAsync();
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var records = await query
            .OrderByDescending(m => m.VisitDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var record = await _context.MedicalRecords
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .ThenInclude(d => d!.Dept)
            .Include(m => m.IcdCodeNavigation)
            .Include(m => m.Prescriptions)
            .ThenInclude(p => p.PrescriptionDetails)
            .ThenInclude(pd => pd.Medicine)
            .Include(m => m.RecordServices)
            .ThenInclude(rs => rs.Service)
            .Include(m => m.Invoice)
            .ThenInclude(i => i!.InvoiceDetails)
            .FirstOrDefaultAsync(m => m.RecordId == id);

        if (record == null) return NotFound();
        return View(record);
    }
}
