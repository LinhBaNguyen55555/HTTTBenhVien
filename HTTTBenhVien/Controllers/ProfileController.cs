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

    private async Task<bool> IsAuthorizedPatientAsync(int requestedPatientId)
    {
        var loggedInPatientId = GetPatientId();
        if (requestedPatientId == loggedInPatientId) return true;

        // Check if the requested patient is a relative of the logged-in user
        var isRelative = await _context.Patients
            .AnyAsync(p => p.PatientId == requestedPatientId && p.CreatedByPatientId == loggedInPatientId);
        
        return isRelative;
    }

    private async Task PopulateFamilyMembersViewDataAsync(int selectedPatientId)
    {
        var loggedInPatientId = GetPatientId();
        
        // Get primary patient
        var primaryPatient = await _context.Patients.FindAsync(loggedInPatientId);
        
        // Get relatives
        var relatives = await _context.Patients
            .Where(p => p.CreatedByPatientId == loggedInPatientId)
            .OrderBy(p => p.FullName)
            .ToListAsync();

        var family = new List<Patient>();
        if (primaryPatient != null)
        {
            family.Add(primaryPatient);
        }
        family.AddRange(relatives);

        ViewBag.FamilyMembers = family;
        ViewBag.SelectedPatientId = selectedPatientId;
    }

    public async Task<IActionResult> Index(int? patientId)
    {
        var loggedInPatientId = GetPatientId();
        var targetPatientId = patientId ?? loggedInPatientId;

        if (!await IsAuthorizedPatientAsync(targetPatientId))
        {
            return Forbid();
        }

        var patient = await _context.Patients
            .Include(p => p.MedicalRecords)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(p => p.PatientId == targetPatientId);

        if (patient == null) return NotFound();

        await PopulateFamilyMembersViewDataAsync(targetPatientId);
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

    public async Task<IActionResult> MedicalHistory(int? patientId)
    {
        var loggedInPatientId = GetPatientId();
        var targetPatientId = patientId ?? loggedInPatientId;

        if (!await IsAuthorizedPatientAsync(targetPatientId))
        {
            return Forbid();
        }

        var records = await _context.MedicalRecords
            .Include(r => r.Doctor)
            .Include(r => r.IcdCodeNavigation)
            .Where(r => r.PatientId == targetPatientId)
            .OrderByDescending(r => r.VisitDate)
            .ToListAsync();

        await PopulateFamilyMembersViewDataAsync(targetPatientId);
        return View(records);
    }

    public async Task<IActionResult> RecordDetails(int id)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Include(r => r.IcdCodeNavigation)
            .Include(r => r.Prescriptions).ThenInclude(p => p.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
            .Include(r => r.RecordServices).ThenInclude(rs => rs.Service)
            .Include(r => r.RecordServices).ThenInclude(rs => rs.Doctor)
            .FirstOrDefaultAsync(r => r.RecordId == id);

        if (record == null) return NotFound();

        // Check security gate using the record's PatientId
        if (!await IsAuthorizedPatientAsync(record.PatientId))
        {
            return Forbid();
        }

        return View(record);
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var loggedInPatientId = GetPatientId();
        if (loggedInPatientId == 0) return Json(new { success = false, message = "Chưa đăng nhập" });


        var patientIds = await _context.Patients
            .Where(p => p.PatientId == loggedInPatientId || p.CreatedByPatientId == loggedInPatientId)
            .Select(p => p.PatientId)
            .ToListAsync();

        var notifications = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Where(a => patientIds.Contains(a.PatientId) && 
                        (a.Status == "Confirmed" || a.Status == "DaXacNhan" || a.Status == "Đã xác nhận" || a.Status == "Xác nhận") && 
                        a.AppDate.HasValue && a.AppDate.Value.Date >= DateTime.Today)
            .OrderByDescending(a => a.AppDate)
            .Select(a => new {
                appointmentId = a.AppId,
                patientId = a.PatientId,
                patientName = a.Patient.FullName,
                date = a.AppDate.Value.ToString("dd/MM/yyyy"),
                time = a.TimeSlot,
                doctorName = a.Doctor != null ? a.Doctor.FullName : "Bác sĩ phòng khám",
                status = a.Status
            })
            .ToListAsync();

        return Json(new { success = true, data = notifications });
    }
}
