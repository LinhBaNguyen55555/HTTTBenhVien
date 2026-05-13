using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers;

[Authorize(Roles = "Patient")]
public class BookingController : Controller
{
    private readonly HtttbenhvienContext _context;

    public BookingController(HtttbenhvienContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var departments = await _context.Departments
            .Include(d => d.DeptTypeNavigation)
            .Where(d => d.DeptTypeNavigation != null && d.DeptTypeNavigation.DeptTypeName == "Lâm sàng")
            .OrderBy(d => d.DeptName)
            .ToListAsync();

        var doctors = await _context.Employees
            .Include(e => e.Dept)
            .Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ"))
            .OrderBy(e => e.FullName)
            .ToListAsync();

        ViewBag.Departments = departments;
        ViewBag.Doctors = doctors;

        var model = new Appointment
        {
            Patient = new Patient()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Appointment model)
    {
        if (!ModelState.IsValid)
        {
            var depts = await _context.Departments.Include(d => d.DeptTypeNavigation).Where(d => d.DeptTypeNavigation != null && d.DeptTypeNavigation.DeptTypeName == "Lâm sàng").OrderBy(d => d.DeptName).ToListAsync();
            var doctors = await _context.Employees.Include(e => e.Dept).Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ")).OrderBy(e => e.FullName).ToListAsync();
            ViewBag.Departments = depts;
            ViewBag.Doctors = doctors;
            return View(model);
        }

        var existingCount = await _context.Appointments
            .CountAsync(a => a.DoctorId == model.DoctorId
                && a.AppDate.HasValue && a.AppDate.Value.Date == model.AppDate.Value.Date
                && a.Status != "Huy");

        if (existingCount >= 15)
        {
            ModelState.AddModelError("", "Bác sĩ đã hết chỗ khám cho ngày này. Vui lòng chọn ngày hoặc bác sĩ khác.");
            var depts = await _context.Departments.Include(d => d.DeptTypeNavigation).Where(d => d.DeptTypeNavigation != null && d.DeptTypeNavigation.DeptTypeName == "Lâm sàng").OrderBy(d => d.DeptName).ToListAsync();
            var doctors = await _context.Employees.Include(e => e.Dept).Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ")).OrderBy(e => e.FullName).ToListAsync();
            ViewBag.Departments = depts;
            ViewBag.Doctors = doctors;
            return View(model);
        }

        model.PatientId = int.Parse(User.FindFirstValue("PatientId") ?? "0");
        model.Status = "DaXacNhan";
        model.CreatedDate = DateTime.Now;
        
        _context.Appointments.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đặt lịch khám thành công! Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.";
        return RedirectToAction("Index");
    }
}
