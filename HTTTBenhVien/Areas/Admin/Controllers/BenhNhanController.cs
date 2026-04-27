using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class BenhNhanController : Controller
{
    private readonly HtttbenhvienContext _context;

    public BenhNhanController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? gender, int page = 1)
    {
        int pageSize = 20;
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(p =>
                p.FullName!.ToLower().Contains(search) ||
                (p.Phone != null && p.Phone.Contains(search)) ||
                (p.BhytCode != null && p.BhytCode.Contains(search)));
        }

        if (!string.IsNullOrEmpty(gender))
            query = query.Where(p => p.Gender == gender);

        ViewBag.Search = search;
        ViewBag.Gender = gender;
        ViewBag.Page = page;

        var total = await query.CountAsync();
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var patients = await query
            .OrderByDescending(p => p.PatientId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(patients);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new Patient();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Patient model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var today = DateTime.Today;
        var todayStr = today.ToString("yyyyMMdd");
        var countToday = await _context.Patients
            .CountAsync(p => p.PatientCode != null && p.PatientCode.Contains(todayStr));
        var newCode = $"BN-{todayStr}-{(countToday + 1):D4}";

        model.PatientCode = newCode;
        _context.Patients.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm bệnh nhân {model.FullName} (Mã: {newCode})";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null) return NotFound();
        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Patient model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _context.Patients.Update(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật thông tin bệnh nhân thành công.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.Appointments)
            .ThenInclude(a => a.Doctor)
            .Include(p => p.MedicalRecords)
            .ThenInclude(m => m.Doctor)
            .Include(p => p.Invoices)
            .FirstOrDefaultAsync(p => p.PatientId == id);

        if (patient == null) return NotFound();
        return View(patient);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null) return NotFound();

        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa bệnh nhân {patient.FullName}.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Search(string? keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return Json(new { results = new object[] { } });

        keyword = keyword.Trim().ToLower();
        var results = await _context.Patients
            .Where(p => p.FullName!.ToLower().Contains(keyword)
                || (p.Phone != null && p.Phone.Contains(keyword))
                || (p.PatientCode != null && p.PatientCode.ToLower().Contains(keyword)))
            .Take(10)
            .Select(p => new { id = p.PatientId, text = $"{p.PatientCode ?? ""} - {p.FullName} - {p.Phone ?? ""}" })
            .ToListAsync();

        return Json(new { results });
    }
}
