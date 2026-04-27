using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ChiDinhController : Controller
{
    private readonly HtttbenhvienContext _context;

    public ChiDinhController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var query = _context.RecordServices
            .Include(rs => rs.Record)
            .Include(rs => rs.Service)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(rs => rs.Status == status);

        ViewBag.Status = status;
        var services = await query.OrderByDescending(rs => rs.ExecuteDate).ToListAsync();
        return View(services);
    }

    [HttpGet]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var rs = await _context.RecordServices.FindAsync(id);
        if (rs == null) return NotFound();

        rs.Status = status;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cap nhat trang thai thanh cong.";
        return RedirectToAction("Index");
    }
}
