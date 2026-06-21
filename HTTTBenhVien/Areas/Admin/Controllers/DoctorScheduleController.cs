using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;
using System.Security.Claims;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DoctorScheduleController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public DoctorScheduleController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/DoctorSchedule

        public async Task<IActionResult> Index(DateTime? filterDate, int? doctorId)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Lễ tân") && !User.IsInRole("LeTan") && !User.IsInRole("Receptionist"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var query = _context.DoctorSchedules.Include(d => d.Doctor).AsQueryable();

            if (filterDate.HasValue)
            {
                query = query.Where(d => d.WorkDate.Date == filterDate.Value.Date);
            }
            
            if (doctorId.HasValue)
            {
                query = query.Where(d => d.DoctorId == doctorId);
            }

            var doctors = await _context.Employees
                .Where(e => (e.JobTitle != null && (e.JobTitle.Contains("Bác sĩ") || e.JobTitle.Contains("Doctor"))) || (e.Account != null && (e.Account.RoleName == "Bác sĩ" || e.Account.RoleName == "BacSi" || e.Account.RoleName == "Doctor")))
                .Select(e => new { e.EmpId, Display = e.FullName + " (" + (e.Specialty ?? "N/A") + ")" })
                .ToListAsync();
            
            ViewData["DoctorId"] = new SelectList(doctors, "EmpId", "Display", doctorId);
            ViewData["FilterDate"] = filterDate?.ToString("yyyy-MM-dd");

            var schedules = await query.OrderByDescending(d => d.WorkDate).ToListAsync();
            return View(schedules);
        }

        // GET: Admin/DoctorSchedule/Personal

        public async Task<IActionResult> Personal()
        {
            if (!User.IsInRole("Bác sĩ") && !User.IsInRole("BacSi") && !User.IsInRole("Doctor"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var empIdClaim = User.FindFirstValue("EmpId");
            if (string.IsNullOrEmpty(empIdClaim) || !int.TryParse(empIdClaim, out int currentEmpId))
            {
                return RedirectToAction("Login", "Account");
            }

            var schedules = await _context.DoctorSchedules
                .Include(d => d.Doctor)
                .Where(d => d.DoctorId == currentEmpId && d.WorkDate >= DateTime.Today)
                .OrderBy(d => d.WorkDate)
                .ThenBy(d => d.ShiftType)
                .ToListAsync();

            return View(schedules);
        }

        // GET: Admin/DoctorSchedule/Create
        public IActionResult Create()
        {
            if (!User.IsInRole("Admin")) return RedirectToAction("AccessDenied", "Account");

            var doctors = _context.Employees
                .Where(e => (e.JobTitle != null && (e.JobTitle.Contains("Bác sĩ") || e.JobTitle.Contains("Doctor"))) || (e.Account != null && (e.Account.RoleName == "Bác sĩ" || e.Account.RoleName == "BacSi" || e.Account.RoleName == "Doctor")))
                .Select(e => new { e.EmpId, Display = e.FullName + " (" + (e.Specialty ?? "N/A") + ")" })
                .ToList();
            
            ViewData["DoctorId"] = new SelectList(doctors, "EmpId", "Display");
            return View(new DoctorSchedule { WorkDate = DateTime.Today.AddDays(1), MaxAppointments = 20, IsAvailable = true });
        }

        // POST: Admin/DoctorSchedule/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorSchedule doctorSchedule)
        {
            if (!User.IsInRole("Admin")) return RedirectToAction("AccessDenied", "Account");

            ModelState.Remove("Doctor");
            ModelState.Remove("Appointments");

            if (ModelState.IsValid)
            {
                var exists = await _context.DoctorSchedules.AnyAsync(s => 
                    s.DoctorId == doctorSchedule.DoctorId && 
                    s.WorkDate.Date == doctorSchedule.WorkDate.Date && 
                    s.ShiftType == doctorSchedule.ShiftType);
                
                if (exists)
                {
                    ModelState.AddModelError("", "Bác sĩ này đã có lịch làm việc trong ca và ngày được chọn.");
                }
                else
                {
                    _context.Add(doctorSchedule);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Thêm lịch làm việc thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            var doctors = _context.Employees
                .Where(e => (e.JobTitle != null && (e.JobTitle.Contains("Bác sĩ") || e.JobTitle.Contains("Doctor"))) || (e.Account != null && (e.Account.RoleName == "Bác sĩ" || e.Account.RoleName == "BacSi" || e.Account.RoleName == "Doctor")))
                .Select(e => new { e.EmpId, Display = e.FullName + " (" + (e.Specialty ?? "N/A") + ")" })
                .ToList();
            ViewData["DoctorId"] = new SelectList(doctors, "EmpId", "Display", doctorSchedule.DoctorId);
            return View(doctorSchedule);
        }

        // GET: Admin/DoctorSchedule/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!User.IsInRole("Admin")) return RedirectToAction("AccessDenied", "Account");

            if (id == null) return NotFound();

            var doctorSchedule = await _context.DoctorSchedules.FindAsync(id);
            if (doctorSchedule == null) return NotFound();

            var doctors = _context.Employees
                .Where(e => (e.JobTitle != null && (e.JobTitle.Contains("Bác sĩ") || e.JobTitle.Contains("Doctor"))) || (e.Account != null && (e.Account.RoleName == "Bác sĩ" || e.Account.RoleName == "BacSi" || e.Account.RoleName == "Doctor")))
                .Select(e => new { e.EmpId, Display = e.FullName + " (" + (e.Specialty ?? "N/A") + ")" })
                .ToList();
            ViewData["DoctorId"] = new SelectList(doctors, "EmpId", "Display", doctorSchedule.DoctorId);
            return View(doctorSchedule);
        }

        // POST: Admin/DoctorSchedule/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorSchedule doctorSchedule)
        {
            if (!User.IsInRole("Admin")) return RedirectToAction("AccessDenied", "Account");

            if (id != doctorSchedule.ScheduleId) return NotFound();

            ModelState.Remove("Doctor");
            ModelState.Remove("Appointments");

            if (ModelState.IsValid)
            {
                try
                {
                    var exists = await _context.DoctorSchedules.AnyAsync(s => 
                        s.ScheduleId != id &&
                        s.DoctorId == doctorSchedule.DoctorId && 
                        s.WorkDate.Date == doctorSchedule.WorkDate.Date && 
                        s.ShiftType == doctorSchedule.ShiftType);
                    
                    if (exists)
                    {
                        ModelState.AddModelError("", "Bác sĩ này đã có lịch làm việc trong ca và ngày được chọn.");
                    }
                    else
                    {
                        _context.Update(doctorSchedule);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Cập nhật lịch làm việc thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorScheduleExists(doctorSchedule.ScheduleId)) return NotFound();
                    else throw;
                }
            }

            var doctors = _context.Employees
                .Where(e => (e.JobTitle != null && (e.JobTitle.Contains("Bác sĩ") || e.JobTitle.Contains("Doctor"))) || (e.Account != null && (e.Account.RoleName == "Bác sĩ" || e.Account.RoleName == "BacSi" || e.Account.RoleName == "Doctor")))
                .Select(e => new { e.EmpId, Display = e.FullName + " (" + (e.Specialty ?? "N/A") + ")" })
                .ToList();
            ViewData["DoctorId"] = new SelectList(doctors, "EmpId", "Display", doctorSchedule.DoctorId);
            return View(doctorSchedule);
        }

        // GET: Admin/DoctorSchedule/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!User.IsInRole("Admin")) return RedirectToAction("AccessDenied", "Account");

            if (id == null) return NotFound();

            var doctorSchedule = await _context.DoctorSchedules
                .Include(d => d.Doctor)
                .FirstOrDefaultAsync(m => m.ScheduleId == id);
            
            if (doctorSchedule == null) return NotFound();

            return View(doctorSchedule);
        }

        // POST: Admin/DoctorSchedule/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!User.IsInRole("Admin")) return RedirectToAction("AccessDenied", "Account");

            var doctorSchedule = await _context.DoctorSchedules.FindAsync(id);
            if (doctorSchedule != null)
            {
                _context.DoctorSchedules.Remove(doctorSchedule);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa lịch làm việc thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorScheduleExists(int id)
        {
            return _context.DoctorSchedules.Any(e => e.ScheduleId == id);
        }
    }
}
