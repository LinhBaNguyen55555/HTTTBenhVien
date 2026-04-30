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
    public class PrescriptionController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public PrescriptionController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/Prescription
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            var prescriptions = _context.Prescriptions
                .Include(p => p.Record).ThenInclude(r => r.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionDetails).ThenInclude(d => d.Medicine)
                .OrderByDescending(p => p.CreateDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                prescriptions = prescriptions.Where(p =>
                    p.Record.Patient.FullName.Contains(searchString) ||
                    (p.Doctor != null && p.Doctor.FullName.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                prescriptions = prescriptions.Where(p => p.Status == statusFilter);
            }

            return View(await prescriptions.ToListAsync());
        }

        // GET: Admin/Prescription/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var prescription = await _context.Prescriptions
                .Include(p => p.Record).ThenInclude(r => r.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionDetails).ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(m => m.PrescriptionId == id);

            if (prescription == null) return NotFound();

            return View(prescription);
        }

        // GET: Admin/Prescription/Create
        public IActionResult Create()
        {
            var recordList = _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.RecordId, Display = "BA #" + m.RecordId + " - " + m.Patient.FullName })
                .ToList();
            ViewData["RecordId"] = new SelectList(recordList, "RecordId", "Display");
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName");
            ViewData["Medicines"] = _context.Medicines.Where(m => m.IsActive == true && m.StockQuantity > 0).OrderBy(m => m.MedicineName).ToList();
            return View();
        }

        // POST: Admin/Prescription/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int RecordId, int DoctorId, string? Note, string? Status,
            int[] MedicineIds, int[] Quantities, string[] Dosages)
        {
            var prescription = new Prescription
            {
                RecordId = RecordId,
                DoctorId = DoctorId,
                CreateDate = DateTime.Now,
                Note = Note,
                Status = string.IsNullOrEmpty(Status) ? "ChoCapPhat" : Status
            };

            _context.Add(prescription);
            await _context.SaveChangesAsync();

            // Add detail rows
            if (MedicineIds != null)
            {
                for (int i = 0; i < MedicineIds.Length; i++)
                {
                    if (MedicineIds[i] > 0)
                    {
                        var detail = new PrescriptionDetail
                        {
                            PrescriptionId = prescription.PrescriptionId,
                            MedicineId = MedicineIds[i],
                            Quantity = i < Quantities.Length ? Quantities[i] : 1,
                            Dosage = i < Dosages.Length ? Dosages[i] : ""
                        };
                        _context.PrescriptionDetails.Add(detail);
                    }
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Tạo đơn thuốc thành công!";
            return RedirectToAction(nameof(Details), new { id = prescription.PrescriptionId });
        }

        // GET: Admin/Prescription/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails).ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();

            var recordList = _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.RecordId, Display = "BA #" + m.RecordId + " - " + m.Patient.FullName })
                .ToList();
            ViewData["RecordId"] = new SelectList(recordList, "RecordId", "Display", prescription.RecordId);
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", prescription.DoctorId);
            ViewData["Medicines"] = _context.Medicines.Where(m => m.IsActive == true).OrderBy(m => m.MedicineName).ToList();
            return View(prescription);
        }

        // POST: Admin/Prescription/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int RecordId, int DoctorId, string? Note, string? Status,
            int[] MedicineIds, int[] Quantities, string[] Dosages)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null) return NotFound();

            prescription.RecordId = RecordId;
            prescription.DoctorId = DoctorId;
            prescription.Note = Note;
            prescription.Status = Status;

            // Remove old details
            _context.PrescriptionDetails.RemoveRange(prescription.PrescriptionDetails);

            // Add new details
            if (MedicineIds != null)
            {
                for (int i = 0; i < MedicineIds.Length; i++)
                {
                    if (MedicineIds[i] > 0)
                    {
                        var detail = new PrescriptionDetail
                        {
                            PrescriptionId = id,
                            MedicineId = MedicineIds[i],
                            Quantity = i < Quantities.Length ? Quantities[i] : 1,
                            Dosage = i < Dosages.Length ? Dosages[i] : ""
                        };
                        _context.PrescriptionDetails.Add(detail);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật đơn thuốc thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Prescription/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var prescription = await _context.Prescriptions
                .Include(p => p.Record).ThenInclude(r => r.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionDetails).ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(m => m.PrescriptionId == id);

            if (prescription == null) return NotFound();
            return View(prescription);
        }

        // POST: Admin/Prescription/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription != null)
            {
                _context.PrescriptionDetails.RemoveRange(prescription.PrescriptionDetails);
                _context.Prescriptions.Remove(prescription);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa đơn thuốc thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
