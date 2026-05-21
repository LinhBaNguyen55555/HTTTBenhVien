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

        if (model.AppDate.HasValue)
        {
            var bookingDate = model.AppDate.Value.Date;
            if (bookingDate < DateTime.Today)
            {
                ModelState.AddModelError("", "Không thể đặt lịch hẹn cho ngày đã qua. Vui lòng chọn ngày khám từ hôm nay trở đi.");
                var depts = await _context.Departments.Include(d => d.DeptTypeNavigation).Where(d => d.DeptTypeNavigation != null && d.DeptTypeNavigation.DeptTypeName == "Lâm sàng").OrderBy(d => d.DeptName).ToListAsync();
                var doctors = await _context.Employees.Include(e => e.Dept).Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ")).OrderBy(e => e.FullName).ToListAsync();
                ViewBag.Departments = depts;
                ViewBag.Doctors = doctors;
                return View(model);
            }

            if (bookingDate == DateTime.Today && !string.IsNullOrEmpty(model.TimeSlot))
            {
                if (TimeSpan.TryParse(model.TimeSlot, out var slotTime))
                {
                    if (DateTime.Now.TimeOfDay >= slotTime)
                    {
                        ModelState.AddModelError("", "Khung giờ bạn chọn cho ngày hôm nay đã trôi qua. Vui lòng chọn khung giờ khác muộn hơn.");
                        var depts = await _context.Departments.Include(d => d.DeptTypeNavigation).Where(d => d.DeptTypeNavigation != null && d.DeptTypeNavigation.DeptTypeName == "Lâm sàng").OrderBy(d => d.DeptName).ToListAsync();
                        var doctors = await _context.Employees.Include(e => e.Dept).Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ")).OrderBy(e => e.FullName).ToListAsync();
                        ViewBag.Departments = depts;
                        ViewBag.Doctors = doctors;
                        return View(model);
                    }
                }
            }
        }

        var existingSlotCount = await _context.Appointments
            .CountAsync(a => a.DoctorId == model.DoctorId
                && a.AppDate.HasValue && a.AppDate.Value.Date == model.AppDate.Value.Date
                && a.TimeSlot == model.TimeSlot
                && a.Status != "Huy");

        if (existingSlotCount >= 15)
        {
            ModelState.AddModelError("", $"Khung giờ {model.TimeSlot} của bác sĩ trong ngày này đã đầy chỗ (tối đa 15 người). Vui lòng chọn khung giờ hoặc bác sĩ khác.");
            var depts = await _context.Departments.Include(d => d.DeptTypeNavigation).Where(d => d.DeptTypeNavigation != null && d.DeptTypeNavigation.DeptTypeName == "Lâm sàng").OrderBy(d => d.DeptName).ToListAsync();
            var doctors = await _context.Employees.Include(e => e.Dept).Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ")).OrderBy(e => e.FullName).ToListAsync();
            ViewBag.Departments = depts;
            ViewBag.Doctors = doctors;
            return View(model);
        }

        var patientId = int.Parse(User.FindFirstValue("PatientId") ?? "0");

        // Cập nhật thông tin bổ sung cho bệnh nhân từ form đặt lịch
        if (model.Patient != null && patientId > 0)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient != null)
            {
                if (!string.IsNullOrWhiteSpace(model.Patient.FullName)) patient.FullName = model.Patient.FullName;
                if (!string.IsNullOrWhiteSpace(model.Patient.Phone)) patient.Phone = model.Patient.Phone;
                if (!string.IsNullOrWhiteSpace(model.Patient.Email)) patient.Email = model.Patient.Email;
                if (!string.IsNullOrWhiteSpace(model.Patient.Address)) patient.Address = model.Patient.Address;
                if (!string.IsNullOrWhiteSpace(model.Patient.CitizenId)) patient.CitizenId = model.Patient.CitizenId;
                if (!string.IsNullOrWhiteSpace(model.Patient.Occupation)) patient.Occupation = model.Patient.Occupation;
                if (!string.IsNullOrWhiteSpace(model.Patient.WorkplaceOrSchool)) patient.WorkplaceOrSchool = model.Patient.WorkplaceOrSchool;
                if (!string.IsNullOrWhiteSpace(model.Patient.BhytCode)) patient.BhytCode = model.Patient.BhytCode;
                if (model.Patient.Dob.HasValue) patient.Dob = model.Patient.Dob;
                patient.Gender = model.Patient.Gender;
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
            }
        }
        
        var newAppointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = model.DoctorId,
            AppDate = model.AppDate,
            TimeSlot = model.TimeSlot,
            Reason = model.Reason,
            Status = "DaXacNhan",
            CreatedDate = DateTime.Now,
            ScheduleId = model.ScheduleId
        };

        _context.Appointments.Add(newAppointment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đặt lịch khám thành công! Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.";
        return RedirectToAction("Index");
    }
}
