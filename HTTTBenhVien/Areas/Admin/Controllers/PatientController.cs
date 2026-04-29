using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PatientController : Controller
    {
        private readonly HtttbenhvienContext _context;
        private readonly IWebHostEnvironment _env;

        public PatientController(HtttbenhvienContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/Patient
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var patients = _context.Patients.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                patients = patients.Where(p => 
                    p.FullName.Contains(searchString) || 
                    (p.CitizenId != null && p.CitizenId.Contains(searchString)) ||
                    (p.PatientCode != null && p.PatientCode.Contains(searchString)));
            }

            return View(await patients.ToListAsync());
        }

        // GET: Admin/Patient/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients.FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // GET: Admin/Patient/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Patient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,FullName,Dob,Gender,Address,Phone,BhytCode,Email,Notes,CitizenId,FamilyMedicalHistory,PersonalMedicalHistory,Occupation,WorkplaceOrSchool")] Patient patient, IFormFile? AvatarUpload)
        {
            ModelState.Remove("Appointments");
            ModelState.Remove("Invoices");
            ModelState.Remove("MedicalRecords");

            if (ModelState.IsValid)
            {
                // Generate PatientCode: BN + year + month + random 4 digits
                string prefix = $"BN{DateTime.Now:yyyyMM}";
                string randomStr = new Random().Next(1000, 9999).ToString();
                patient.PatientCode = $"{prefix}-{randomStr}";

                if (AvatarUpload != null && AvatarUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "assets", "img", "imagepatient");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(AvatarUpload.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await AvatarUpload.CopyToAsync(fileStream);
                    }
                    patient.AvatarUrl = uniqueFileName;
                }

                _context.Add(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm hồ sơ bệnh nhân thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // GET: Admin/Patient/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // POST: Admin/Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PatientId,PatientCode,FullName,Dob,Gender,Address,Phone,BhytCode,Email,AvatarUrl,Notes,CitizenId,FamilyMedicalHistory,PersonalMedicalHistory,Occupation,WorkplaceOrSchool")] Patient patient, IFormFile? AvatarUpload, bool RemoveAvatar = false)
        {
            if (id != patient.PatientId) return NotFound();

            ModelState.Remove("Appointments");
            ModelState.Remove("Invoices");
            ModelState.Remove("MedicalRecords");

            if (ModelState.IsValid)
            {
                try
                {
                    if (RemoveAvatar)
                    {
                        patient.AvatarUrl = null;
                    }

                    if (AvatarUpload != null && AvatarUpload.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_env.WebRootPath, "assets", "img", "imagepatient");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(AvatarUpload.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await AvatarUpload.CopyToAsync(fileStream);
                        }
                        patient.AvatarUrl = uniqueFileName;
                    }

                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật hồ sơ bệnh nhân thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // GET: Admin/Patient/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients.FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // POST: Admin/Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                try
                {
                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Xóa hồ sơ bệnh nhân thành công!";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Không thể xóa bệnh nhân này vì đã có lịch hẹn hoặc hóa đơn liên quan!";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }
    }
}
