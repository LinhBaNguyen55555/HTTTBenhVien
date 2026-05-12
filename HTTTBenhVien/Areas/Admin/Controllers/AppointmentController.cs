using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AppointmentController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public AppointmentController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/LichKham
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            var appointments = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .AsQueryable();

            if (User.IsInRole("BacSi") || User.IsInRole("Doctor") || User.IsInRole("Bác sĩ"))
            {
                var empIdClaim = User.FindFirst("EmpId");
                if (empIdClaim != null && int.TryParse(empIdClaim.Value, out int empId))
                {
                    appointments = appointments.Where(a => a.DoctorId == empId);
                }
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                appointments = appointments.Where(a => 
                    a.Patient.FullName.Contains(searchString) || 
                    (a.Doctor != null && a.Doctor.FullName.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                appointments = appointments.Where(a => a.Status == statusFilter);
            }

            // Order by date descending
            appointments = appointments.OrderByDescending(a => a.AppDate);

            return View(await appointments.ToListAsync());
        }

        // GET: Admin/LichKham/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.AppId == id);
            
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // GET: Admin/LichKham/Create
        public async Task<IActionResult> Create()
        {
            // Lấy các ca trực từ hôm nay trở đi
            var schedules = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .Where(s => s.WorkDate >= DateTime.Today && s.IsAvailable == true)
                .OrderBy(s => s.WorkDate)
                .Select(s => new {
                    s.ScheduleId,
                    Display = $"{s.Doctor.FullName} - {s.WorkDate:dd/MM/yyyy} - Ca {s.ShiftType}"
                })
                .ToListAsync();

            ViewData["ScheduleId"] = new SelectList(schedules, "ScheduleId", "Display");
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName");
            return View();
        }

        // GET: Admin/LichKham/GetTimeSlots
        [HttpGet]
        public async Task<IActionResult> GetTimeSlots(int scheduleId)
        {
            var schedule = await _context.DoctorSchedules.FindAsync(scheduleId);
            if (schedule == null) return Json(new List<string>());

            var timeSlots = new List<string>();
            if (schedule.ShiftType == "Sáng")
            {
                timeSlots.AddRange(new[] { "07:30", "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00" });
            }
            else if (schedule.ShiftType == "Chiều")
            {
                timeSlots.AddRange(new[] { "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00" });
            }
            else if (schedule.ShiftType == "Tối")
            {
                timeSlots.AddRange(new[] { "18:00", "18:30", "19:00", "19:30", "20:00", "20:30" });
            }

            return Json(timeSlots);
        }

        // POST: Admin/LichKham/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppId,PatientId,ScheduleId,TimeSlot,Reason,Status,Notes")] Appointment appointment)
        {
            ModelState.Remove("Doctor");
            ModelState.Remove("Patient");
            ModelState.Remove("Schedule");

            if (ModelState.IsValid)
            {
                var schedule = await _context.DoctorSchedules.FindAsync(appointment.ScheduleId);
                if (schedule != null)
                {
                    appointment.DoctorId = schedule.DoctorId;
                    appointment.AppDate = schedule.WorkDate;

                    // Xếp số thứ tự
                    int currentCount = await _context.Appointments.CountAsync(a => a.ScheduleId == schedule.ScheduleId);
                    int queueNumber = currentCount + 1;
                    
                    // Lưu số thứ tự vào TimeSlot hoặc Notes để hiển thị
                    appointment.Notes = $"[STT: {queueNumber}] {appointment.Notes}";
                }

                appointment.CreatedDate = DateTime.Now;
                if (string.IsNullOrEmpty(appointment.Status))
                {
                    appointment.Status = "New";
                }

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm lịch hẹn thành công!";
                return RedirectToAction(nameof(Index));
            }

            var schedules = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .Where(s => s.WorkDate >= DateTime.Today && s.IsAvailable == true)
                .OrderBy(s => s.WorkDate)
                .Select(s => new {
                    s.ScheduleId,
                    Display = $"{s.Doctor.FullName} - {s.WorkDate:dd/MM/yyyy} - Ca {s.ShiftType}"
                })
                .ToListAsync();

            ViewData["ScheduleId"] = new SelectList(schedules, "ScheduleId", "Display", appointment.ScheduleId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", appointment.PatientId);
            return View(appointment);
        }

        // GET: Admin/LichKham/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var schedules = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .Where(s => s.WorkDate >= DateTime.Today || s.ScheduleId == appointment.ScheduleId)
                .OrderByDescending(s => s.WorkDate)
                .Select(s => new {
                    s.ScheduleId,
                    Display = $"{s.Doctor.FullName} - {s.WorkDate:dd/MM/yyyy} - Ca {s.ShiftType}"
                })
                .ToListAsync();

            ViewData["ScheduleId"] = new SelectList(schedules, "ScheduleId", "Display", appointment.ScheduleId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", appointment.PatientId);
            return View(appointment);
        }

        // POST: Admin/LichKham/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppId,PatientId,ScheduleId,TimeSlot,Reason,Status,CreatedDate,Notes")] Appointment appointment)
        {
            if (id != appointment.AppId) return NotFound();

            ModelState.Remove("Doctor");
            ModelState.Remove("Patient");
            ModelState.Remove("Schedule");

            if (ModelState.IsValid)
            {
                try
                {
                    var schedule = await _context.DoctorSchedules.FindAsync(appointment.ScheduleId);
                    if (schedule != null)
                    {
                        appointment.DoctorId = schedule.DoctorId;
                        appointment.AppDate = schedule.WorkDate;
                    }

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật lịch hẹn thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var schedules = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .Where(s => s.WorkDate >= DateTime.Today || s.ScheduleId == appointment.ScheduleId)
                .OrderByDescending(s => s.WorkDate)
                .Select(s => new {
                    s.ScheduleId,
                    Display = $"{s.Doctor.FullName} - {s.WorkDate:dd/MM/yyyy} - Ca {s.ShiftType}"
                })
                .ToListAsync();

            ViewData["ScheduleId"] = new SelectList(schedules, "ScheduleId", "Display", appointment.ScheduleId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", appointment.PatientId);
            return View(appointment);
        }

        // GET: Admin/LichKham/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.AppId == id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // POST: Admin/LichKham/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa lịch hẹn thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppId == id);
        }
    }
}

