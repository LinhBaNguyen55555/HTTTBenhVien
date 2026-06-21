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
    public class MedicalRecordController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public MedicalRecordController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/MedicalRecord
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var records = _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.IcdCodeNavigation)
                .OrderByDescending(m => m.VisitDate)
                .AsQueryable();

            if (User.IsInRole("BacSi") || User.IsInRole("Doctor") || User.IsInRole("Bác sĩ"))
            {
                var empIdClaim = User.FindFirst("EmpId");
                if (empIdClaim != null && int.TryParse(empIdClaim.Value, out int empId))
                {
                    records = records.Where(m => m.DoctorId == empId);
                }
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                records = records.Where(m =>
                    m.Patient.FullName.Contains(searchString) ||
                    (m.Doctor != null && m.Doctor.FullName.Contains(searchString)) ||
                    (m.IcdCode != null && m.IcdCode.Contains(searchString)) ||
                    (m.Diagnosis != null && m.Diagnosis.Contains(searchString)));
            }

            return View(await records.ToListAsync());
        }

        // GET: Admin/MedicalRecord/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.IcdCodeNavigation)
                .Include(m => m.Prescriptions)
                .FirstOrDefaultAsync(m => m.RecordId == id);

            if (record == null) return NotFound();

            return View(record);
        }

        // GET: Admin/MedicalRecord/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName");
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName");
            ViewData["IcdCode"] = new SelectList(_context.IcdDictionaries.Where(i => i.IsActive == true), "IcdCode", "DiseaseName");
            return View();
        }

        // POST: Admin/MedicalRecord/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RecordId,PatientId,DoctorId,AppointmentId,VisitDate,Symptoms,Diagnosis,Conclusion,RecordType,IcdCode,Notes,VitalSigns,BloodPressure,PulseRate,Temperature,Height,Weight,SpO2")] MedicalRecord record)
        {
            ModelState.Remove("Doctor");
            ModelState.Remove("Patient");
            ModelState.Remove("IcdCodeNavigation");
            ModelState.Remove("Invoice");
            ModelState.Remove("Prescriptions");
            ModelState.Remove("RecordServices");

            if (ModelState.IsValid)
            {
                if (record.VisitDate == null)
                {
                    record.VisitDate = DateTime.Now;
                }

                _context.Add(record);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo bệnh án thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", record.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", record.DoctorId);
            ViewData["IcdCode"] = new SelectList(_context.IcdDictionaries.Where(i => i.IsActive == true), "IcdCode", "DiseaseName", record.IcdCode);
            return View(record);
        }

        // GET: Admin/MedicalRecord/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null) return NotFound();

            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", record.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", record.DoctorId);
            ViewData["IcdCode"] = new SelectList(_context.IcdDictionaries.Where(i => i.IsActive == true), "IcdCode", "DiseaseName", record.IcdCode);
            return View(record);
        }

        // POST: Admin/MedicalRecord/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RecordId,PatientId,DoctorId,AppointmentId,VisitDate,Symptoms,Diagnosis,Conclusion,RecordType,IcdCode,Notes,VitalSigns,BloodPressure,PulseRate,Temperature,Height,Weight,SpO2")] MedicalRecord record)
        {
            if (id != record.RecordId) return NotFound();

            ModelState.Remove("Doctor");
            ModelState.Remove("Patient");
            ModelState.Remove("IcdCodeNavigation");
            ModelState.Remove("Invoice");
            ModelState.Remove("Prescriptions");
            ModelState.Remove("RecordServices");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(record);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật bệnh án thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecordExists(record.RecordId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", record.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", record.DoctorId);
            ViewData["IcdCode"] = new SelectList(_context.IcdDictionaries.Where(i => i.IsActive == true), "IcdCode", "DiseaseName", record.IcdCode);
            return View(record);
        }

        // GET: Admin/MedicalRecord/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.IcdCodeNavigation)
                .FirstOrDefaultAsync(m => m.RecordId == id);

            if (record == null) return NotFound();

            return View(record);
        }

        // POST: Admin/MedicalRecord/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record != null)
            {
                try
                {
                    _context.MedicalRecords.Remove(record);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa bệnh án thành công!";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Không thể xóa bệnh án này vì đã có đơn thuốc hoặc hóa đơn liên quan!";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RecordExists(int id)
        {
            return _context.MedicalRecords.Any(e => e.RecordId == id);
        }
    }
}
