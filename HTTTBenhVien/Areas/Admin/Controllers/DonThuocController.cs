using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DonThuocController : Controller
{
    private readonly HtttbenhvienContext _context;

    public DonThuocController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? recordId)
    {
        var query = _context.Prescriptions
            .Include(p => p.Record)
            .AsQueryable();

        if (recordId > 0)
            query = query.Where(p => p.RecordId == recordId);

        ViewBag.RecordId = recordId;
        var prescriptions = await query.OrderByDescending(p => p.CreateDate).ToListAsync();
        return View(prescriptions);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Record)
            .Include(p => p.PrescriptionDetails)
            .ThenInclude(pd => pd.Medicine)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);

        if (prescription == null) return NotFound();
        return View(prescription);
    }
}
