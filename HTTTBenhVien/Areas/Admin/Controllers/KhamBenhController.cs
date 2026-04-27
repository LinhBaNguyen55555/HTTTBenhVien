using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class KhamBenhController : Controller
{
    private readonly HtttbenhvienContext _context;

    public KhamBenhController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? appointmentId)
    {
        var today = DateTime.Today;
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.AppDate.HasValue && a.AppDate.Value.Date == today && a.Status == "DangKham")
            .OrderBy(a => a.TimeSlot)
            .ToListAsync();

        ViewBag.AppointmentId = appointmentId;
        return View(appointments);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? appointmentId, int? patientId)
    {
        Appointment? appointment = null;
        Patient? patient = null;

        if (appointmentId > 0)
        {
            appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppId == appointmentId);

            if (appointment != null)
                patient = appointment.Patient;
        }
        else if (patientId > 0)
        {
            patient = await _context.Patients.FindAsync(patientId);
        }

        if (patient == null)
            return RedirectToAction("Index");

        var empIdClaim = User.FindFirst("EmpId")?.Value;
        int empId = 0;
        int.TryParse(empIdClaim, out empId);

        var model = new KhamBenhViewModel
        {
            PatientId = patient.PatientId,
            PatientName = patient.FullName,
            PatientPhone = patient.Phone,
            PatientDob = patient.Dob,
            PatientGender = patient.Gender,
            PatientCode = patient.PatientCode,
            BhytCode = patient.BhytCode,
            AppointmentId = appointmentId,
            DoctorId = appointment?.DoctorId ?? empId,
            DoctorName = appointment?.Doctor?.FullName,
            VisitDate = DateTime.Now,
            RecordType = "NgoaiTru"
        };

        var icds = await _context.IcdDictionaries
            .Where(i => i.IsActive == true)
            .OrderBy(i => i.IcdCode)
            .Take(50)
            .ToListAsync();

        ViewBag.ICDs = icds;
        ViewBag.Medicines = await _context.Medicines.Where(m => m.StockQuantity > 0).OrderBy(m => m.MedicineName).ToListAsync();
        ViewBag.Services = await _context.MedicalServices.OrderBy(s => s.ServiceName).ToListAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KhamBenhViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var record = new MedicalRecord
            {
                PatientId = model.PatientId,
                DoctorId = model.DoctorId ?? 0,
                AppointmentId = model.AppointmentId,
                VisitDate = model.VisitDate,
                Symptoms = model.Symptoms,
                Diagnosis = model.Diagnosis,
                IcdCode = model.IcdCode,
                RecordType = model.RecordType,
                Notes = model.Notes,
                VitalSigns = model.VitalSigns
            };

            _context.MedicalRecords.Add(record);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.MedicineIds))
            {
                var ids = model.MedicineIds.Split(',').Select(int.Parse).ToList();
                var qtys = model.MedicineQtys?.Split(',').Select(int.Parse).ToList() ?? new List<int>();
                var dosages = model.MedicineDosages?.Split('|').ToList() ?? new List<string>();

                var prescription = new Prescription
                {
                    RecordId = record.RecordId,
                    DoctorId = model.DoctorId ?? 0,
                    CreateDate = DateTime.Now,
                    Note = model.PrescriptionNote
                };
                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                for (int i = 0; i < ids.Count; i++)
                {
                    var medId = ids[i];
                    var qty = qtys.Count > i ? qtys[i] : 1;
                    var dosage = dosages.Count > i ? dosages[i] : "";

                    var detail = new PrescriptionDetail
                    {
                        PrescriptionId = prescription.PrescriptionId,
                        MedicineId = medId,
                        Quantity = qty,
                        Dosage = dosage
                    };
                    _context.PrescriptionDetails.Add(detail);

                    var med = await _context.Medicines.FindAsync(medId);
                    if (med != null)
                        med.StockQuantity = Math.Max(0, med.StockQuantity - qty);
                }
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(model.ServiceIds))
            {
                var svcIds = model.ServiceIds.Split(',').Select(int.Parse).ToList();
                foreach (var svcId in svcIds)
                {
                    var rs = new RecordService
                    {
                        RecordId = record.RecordId,
                        ServiceId = svcId,
                        DoctorId = model.DoctorId ?? 0,
                        ExecuteDate = DateTime.Now,
                        Status = "ChoNhanMau"
                    };
                    _context.RecordServices.Add(rs);
                }
                await _context.SaveChangesAsync();
            }

            if (model.AppointmentId > 0)
            {
                var appt = await _context.Appointments.FindAsync(model.AppointmentId);
                if (appt != null)
                    appt.Status = "HoanThanh";
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            TempData["Success"] = "Da luu ho so kham benh cho benh nhan " + model.PatientName;
            return RedirectToAction("Index");
        }
        catch
        {
            await transaction.RollbackAsync();
            await LoadDropdowns();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await _context.MedicalRecords
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Include(m => m.Prescriptions)
            .ThenInclude(p => p.PrescriptionDetails)
            .ThenInclude(pd => pd.Medicine)
            .Include(m => m.RecordServices)
            .ThenInclude(rs => rs.Service)
            .FirstOrDefaultAsync(m => m.RecordId == id);

        if (record == null) return NotFound();

        var model = new KhamBenhViewModel
        {
            RecordId = record.RecordId,
            PatientId = record.PatientId,
            PatientName = record.Patient?.FullName,
            PatientPhone = record.Patient?.Phone,
            PatientDob = record.Patient?.Dob,
            PatientGender = record.Patient?.Gender,
            PatientCode = record.Patient?.PatientCode,
            BhytCode = record.Patient?.BhytCode,
            AppointmentId = record.AppointmentId,
            DoctorId = record.DoctorId,
            DoctorName = record.Doctor?.FullName,
            VisitDate = record.VisitDate,
            Symptoms = record.Symptoms,
            Diagnosis = record.Diagnosis,
            IcdCode = record.IcdCode,
            RecordType = record.RecordType,
            Notes = record.Notes,
            VitalSigns = record.VitalSigns,
            Record = record
        };

        await LoadDropdowns();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var record = await _context.MedicalRecords
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .ThenInclude(d => d!.Dept)
            .Include(m => m.IcdCodeNavigation)
            .Include(m => m.Prescriptions)
            .ThenInclude(p => p.PrescriptionDetails)
            .ThenInclude(pd => pd.Medicine)
            .Include(m => m.RecordServices)
            .ThenInclude(rs => rs.Service)
            .FirstOrDefaultAsync(m => m.RecordId == id);

        if (record == null) return NotFound();
        return View(record);
    }

    private async Task LoadDropdowns()
    {
        ViewBag.ICDs = await _context.IcdDictionaries.Where(i => i.IsActive == true).OrderBy(i => i.IcdCode).ToListAsync();
        ViewBag.Medicines = await _context.Medicines.Where(m => m.StockQuantity > 0).OrderBy(m => m.MedicineName).ToListAsync();
        ViewBag.Services = await _context.MedicalServices.OrderBy(s => s.ServiceName).ToListAsync();
        ViewBag.Doctors = await _context.Employees.Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ")).OrderBy(e => e.FullName).ToListAsync();
    }
}

public class KhamBenhViewModel
{
    public int RecordId { get; set; }
    public int PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientPhone { get; set; }
    public DateTime? PatientDob { get; set; }
    public string? PatientGender { get; set; }
    public string? PatientCode { get; set; }
    public string? BhytCode { get; set; }
    public int? AppointmentId { get; set; }
    public int? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public DateTime? VisitDate { get; set; }
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? IcdCode { get; set; }
    public string? RecordType { get; set; }
    public string? Notes { get; set; }
    public string? VitalSigns { get; set; }
    public string? MedicineIds { get; set; }
    public string? MedicineQtys { get; set; }
    public string? MedicineDosages { get; set; }
    public string? ServiceIds { get; set; }
    public string? PrescriptionNote { get; set; }
    public MedicalRecord? Record { get; set; }
}
