using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class LichKhamController : Controller
{
    private readonly HtttbenhvienContext _context;

    public LichKhamController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status, int? doctorId, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        int pageSize = 25;
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .ThenInclude(d => d!.Dept)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        if (doctorId > 0)
            query = query.Where(a => a.DoctorId == doctorId);

        if (fromDate.HasValue)
            query = query.Where(a => a.AppDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.AppDate <= toDate.Value.AddDays(1));

        ViewBag.Status = status;
        ViewBag.DoctorId = doctorId;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        var total = await query.CountAsync();
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var appointments = await query
            .OrderByDescending(a => a.AppDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Doctors = await _context.Employees
            .Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ"))
            .OrderBy(e => e.FullName).ToListAsync();

        return View(appointments);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var appt = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .ThenInclude(d => d!.Dept)
            .FirstOrDefaultAsync(a => a.AppId == id);

        if (appt == null) return NotFound();
        return View(appt);
    }

    [HttpGet]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null) return NotFound();

        appt.Status = status;
        await _context.SaveChangesAsync();

        var statusText = status switch
        {
            "DaXacNhan" => "Đã xác nhận",
            "DangKham" => "Đang khám",
            "HoanThanh" => "Hoàn thành",
            "Huy" => "Hủy",
            _ => status
        };

        TempData["Success"] = $"Cập nhật trạng thái thành: {statusText}";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null) return NotFound();

        appt.Status = "Huy";
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã hủy lịch hẹn.";
        return RedirectToAction("Index");
    }
}
