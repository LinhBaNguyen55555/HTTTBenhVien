using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers;

[Authorize(Roles = "Patient")]
public class ProfileController : Controller
{
    private readonly HtttbenhvienContext _context;

    public ProfileController(HtttbenhvienContext context)
    {
        _context = context;
    }

    private int GetPatientId()
    {
        return int.Parse(User.FindFirstValue("PatientId") ?? "0");
    }

    public async Task<IActionResult> Index()
    {
        var patientId = GetPatientId();
        var patient = await _context.Patients
            .Include(p => p.MedicalRecords)
            .Include(p => p.Appointments)
            .FirstOrDefaultAsync(p => p.PatientId == patientId);

        if (patient == null) return NotFound();

        return View(patient);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var patientId = GetPatientId();
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return NotFound();
        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Patient model, IFormFile? avatarFile)
    {
        var patientId = GetPatientId();
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return NotFound();

        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            ModelState.AddModelError("FullName", "Vui lòng nhập Họ và tên.");
            return View(model);
        }

        patient.FullName = model.FullName;
        patient.Email = model.Email;
        patient.Address = model.Address;
        patient.Dob = model.Dob;
        patient.Gender = model.Gender;
        patient.Occupation = model.Occupation;
        patient.WorkplaceOrSchool = model.WorkplaceOrSchool;
        patient.PersonalMedicalHistory = model.PersonalMedicalHistory;

        if (avatarFile != null)
        {
            var fileName = "BN_" + patientId + "_" + Path.GetFileName(avatarFile.FileName);
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/assets/img/imagepatient");
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }
            patient.AvatarUrl = fileName;
        }

        _context.Update(patient);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật thông tin thành công!";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> MedicalHistory()
    {
        var patientId = GetPatientId();
        var records = await _context.MedicalRecords
            .Include(r => r.Doctor)
            .Include(r => r.IcdCodeNavigation)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.VisitDate)
            .ToListAsync();
        return View(records);
    }

    public async Task<IActionResult> RecordDetails(int id)
    {
        var patientId = GetPatientId();
        var record = await _context.MedicalRecords
            .Include(r => r.Doctor)
            .Include(r => r.IcdCodeNavigation)
            .Include(r => r.Prescriptions).ThenInclude(p => p.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
            .Include(r => r.RecordServices).ThenInclude(rs => rs.Service)
            .FirstOrDefaultAsync(r => r.RecordId == id && r.PatientId == patientId);

        if (record == null) return NotFound();
        return View(record);
    }
}
