using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class TiepNhanController : Controller
{
    private readonly HtttbenhvienContext _context;

    public TiepNhanController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.AppDate.HasValue && a.AppDate.Value.Date == today)
            .OrderBy(a => a.AppDate)
            .ToListAsync();

        ViewBag.TodayCount = appointments.Count;
        ViewBag.Confirmed = appointments.Count(a => a.Status == "DaXacNhan");
        ViewBag.Completed = appointments.Count(a => a.Status == "HoanThanh");

        return View(appointments);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new TiepNhanViewModel();
        LoadDropdowns();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TiepNhanViewModel model)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            return View(model);
        }

        Patient patient;

        if (model.PatientId > 0)
        {
            patient = await _context.Patients.FindAsync(model.PatientId)
                ?? throw new Exception("Khong tim thay benh nhan");
        }
        else
        {
            var today = DateTime.Today;
            var todayStr = today.ToString("yyyyMMdd");
            var countToday = await _context.Patients
                .CountAsync(p => p.PatientCode != null && p.PatientCode.Contains(todayStr));
            var newCode = $"BN-{todayStr}-{(countToday + 1):D4}";

            patient = new Patient
            {
                PatientCode = newCode,
                FullName = model.FullName ?? "",
                Gender = model.Gender,
                Dob = model.Dob,
                Phone = model.Phone,
                Address = model.Address,
                BhytCode = model.BhytCode
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
        }

        var appointment = new Appointment
        {
            PatientId = patient.PatientId,
            DoctorId = model.DoctorId ?? 0,
            AppDate = model.AppDate,
            TimeSlot = model.TimeSlot,
            Reason = model.Reason,
            Status = "DaXacNhan",
            CreatedDate = DateTime.Now,
            Notes = model.Notes
        };
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Tiep nhan thanh cong! Ma benh nhan: {patient.PatientCode}";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null) return NotFound();

        var model = new TiepNhanViewModel
        {
            PatientId = appt.PatientId,
            DoctorId = appt.DoctorId,
            AppDate = appt.AppDate,
            TimeSlot = appt.TimeSlot,
            Reason = appt.Reason,
            Notes = appt.Notes
        };

        LoadDropdowns();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TiepNhanViewModel model)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null) return NotFound();

        appt.DoctorId = model.DoctorId ?? appt.DoctorId;
        appt.AppDate = model.AppDate;
        appt.TimeSlot = model.TimeSlot;
        appt.Reason = model.Reason;
        appt.Notes = model.Notes;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cap nhat lich hen thanh cong.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int id)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null) return NotFound();

        appt.Status = "DangKham";
        await _context.SaveChangesAsync();

        TempData["Success"] = "Da xac nhan va dua vao danh sach kham.";
        return RedirectToAction("Index");
    }

    private void LoadDropdowns()
    {
        ViewBag.Doctors = _context.Employees
            .Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ"))
            .OrderBy(e => e.FullName).ToList();
    }
}

public class TiepNhanViewModel
{
    public int PatientId { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? BhytCode { get; set; }
    public int? DoctorId { get; set; }
    public DateTime? AppDate { get; set; }
    public string? TimeSlot { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
