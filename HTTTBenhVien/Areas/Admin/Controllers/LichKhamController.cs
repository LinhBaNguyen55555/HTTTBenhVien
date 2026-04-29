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
    public class LichKhamController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public LichKhamController(HtttbenhvienContext context)
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
        public IActionResult Create()
        {
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName");
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName");
            return View();
        }

        // POST: Admin/LichKham/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppId,PatientId,DoctorId,AppDate,TimeSlot,Reason,Status,Notes")] Appointment appointment)
        {
            ModelState.Remove("Doctor");
            ModelState.Remove("Patient");

            if (ModelState.IsValid)
            {
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
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", appointment.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", appointment.PatientId);
            return View(appointment);
        }

        // GET: Admin/LichKham/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", appointment.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", appointment.PatientId);
            return View(appointment);
        }

        // POST: Admin/LichKham/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppId,PatientId,DoctorId,AppDate,TimeSlot,Reason,Status,CreatedDate,Notes")] Appointment appointment)
        {
            if (id != appointment.AppId) return NotFound();

            ModelState.Remove("Doctor");
            ModelState.Remove("Patient");

            if (ModelState.IsValid)
            {
                try
                {
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
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", appointment.DoctorId);
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
