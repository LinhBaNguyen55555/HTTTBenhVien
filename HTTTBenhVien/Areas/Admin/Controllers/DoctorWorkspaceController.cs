using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Bác sĩ,BacSi,Doctor")]
    public class DoctorWorkspaceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public DoctorWorkspaceController(HtttbenhvienContext context)
        {
            _context = context;
        }

        private int GetCurrentEmpId()
        {
            var empIdClaim = User.FindFirst("EmpId");
            if (empIdClaim != null && int.TryParse(empIdClaim.Value, out int empId))
            {
                return empId;
            }
            return 0;
        }

        // GET: Admin/DoctorWorkspace
        public async Task<IActionResult> Index()
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var today = DateTime.Today;

            // Fetch today's appointments for this doctor
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == empId && a.AppDate != null && a.AppDate.Value.Date == today)
                .OrderBy(a => a.TimeSlot)
                .ToListAsync();

            // Fetch today's medical records to know who has been examined
            var medicalRecords = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Where(m => m.DoctorId == empId && m.VisitDate != null && m.VisitDate.Value.Date == today)
                .ToListAsync();

            ViewBag.Appointments = appointments;
            ViewBag.MedicalRecords = medicalRecords;
            ViewBag.TotalPatients = appointments.Count;
            ViewBag.Examined = medicalRecords.Count;
            ViewBag.Waiting = appointments.Count(a => !medicalRecords.Any(m => m.AppointmentId == a.AppId));

            return View();
        }

        // GET: Admin/DoctorWorkspace/Examine/5
        public async Task<IActionResult> Examine(int id)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            // id here is AppId
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppId == id && a.DoctorId == empId);

            if (appointment == null) return NotFound();

            // Check if record already exists
            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Prescriptions)
                    .ThenInclude(p => p.PrescriptionDetails)
                        .ThenInclude(d => d.Medicine)
                .Include(m => m.RecordServices)
                    .ThenInclude(rs => rs.Service)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (record == null)
            {
                record = new MedicalRecord
                {
                    AppointmentId = appointment.AppId,
                    PatientId = appointment.PatientId,
                    DoctorId = empId,
                    VisitDate = DateTime.Now,
                    Patient = appointment.Patient, // for display
                    Notes = appointment.Notes
                };
            }

            ViewBag.IcdCodes = await _context.IcdDictionaries.Where(i => i.IsActive == true).ToListAsync();
            ViewBag.Medicines = await _context.Medicines.Where(m => m.IsActive != false).OrderBy(m => m.MedicineName).ToListAsync();
            ViewBag.MedicalServices = await _context.MedicalServices.OrderBy(s => s.ServiceName).ToListAsync();
            return View(record);
        }

        // POST: Admin/DoctorWorkspace/SaveRecord
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRecord([Bind("RecordId,PatientId,DoctorId,AppointmentId,Symptoms,Diagnosis,Conclusion,RecordType,IcdCode,Notes,BloodPressure,PulseRate,Temperature,Height,Weight,SpO2")] MedicalRecord record)
        {
            ModelState.Remove("Doctor");
            ModelState.Remove("Patient");
            ModelState.Remove("IcdCodeNavigation");
            ModelState.Remove("Invoice");
            ModelState.Remove("Prescriptions");
            ModelState.Remove("RecordServices");

            if (ModelState.IsValid)
            {
                if (record.RecordId == 0)
                {
                    record.VisitDate = DateTime.Now;
                    _context.Add(record);
                    TempData["Success"] = "Đã lưu bệnh án thành công!";
                }
                else
                {
                    var existing = await _context.MedicalRecords.FindAsync(record.RecordId);
                    if (existing != null)
                    {
                        existing.Symptoms = record.Symptoms;
                        existing.Diagnosis = record.Diagnosis;
                        existing.Conclusion = record.Conclusion;
                        existing.IcdCode = record.IcdCode;
                        existing.Notes = record.Notes;
                        existing.BloodPressure = record.BloodPressure;
                        existing.PulseRate = record.PulseRate;
                        existing.Temperature = record.Temperature;
                        existing.Height = record.Height;
                        existing.Weight = record.Weight;
                        existing.SpO2 = record.SpO2;
                        
                        _context.Update(existing);
                        TempData["Success"] = "Đã cập nhật bệnh án thành công!";
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Có lỗi xảy ra khi lưu bệnh án.";
            return RedirectToAction(nameof(Examine), new { id = record.AppointmentId });
        }
        // POST: Admin/DoctorWorkspace/AddPrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrescription(int RecordId, int AppointmentId, int[] MedicineIds, int[] Quantities, string[] Dosages)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            if (RecordId == 0)
            {
                TempData["Error"] = "Vui lòng lưu bệnh án trước khi kê đơn!";
                return RedirectToAction(nameof(Examine), new { id = AppointmentId });
            }

            var prescription = new Prescription
            {
                RecordId = RecordId,
                DoctorId = empId,
                CreateDate = DateTime.Now,
                Status = "ChoCapPhat",
                Note = ""
            };

            _context.Add(prescription);
            await _context.SaveChangesAsync();

            if (MedicineIds != null)
            {
                var record = await _context.MedicalRecords.FindAsync(RecordId);
                var invoice = await GetOrCreateActiveInvoiceAsync(record.PatientId, RecordId);
                var medicines = await _context.Medicines.Where(m => MedicineIds.Contains(m.MedicineId)).ToListAsync();
                decimal addedTotal = 0;

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

                        var med = medicines.FirstOrDefault(m => m.MedicineId == MedicineIds[i]);
                        if (med != null)
                        {
                            var lineTotal = (detail.Quantity ?? 1) * med.Price;
                            addedTotal += lineTotal;

                            var invDetail = new InvoiceDetail
                            {
                                InvoiceId = invoice.InvoiceId,
                                ItemType = "Thuốc",
                                ItemId = med.MedicineId,
                                ItemName = med.MedicineName,
                                Quantity = detail.Quantity,
                                UnitPrice = med.Price
                            };
                            _context.InvoiceDetails.Add(invDetail);
                        }
                    }
                }
                await _context.SaveChangesAsync();

                if (addedTotal > 0)
                {
                    invoice.TotalAmount = (invoice.TotalAmount ?? 0) + addedTotal;
                    invoice.PatientAmount = invoice.TotalAmount - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                    if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;
                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Thêm đơn thuốc thành công!";
            return RedirectToAction(nameof(Examine), new { id = AppointmentId });
        }

        // POST: Admin/DoctorWorkspace/AddRecordService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRecordService(int RecordId, int AppointmentId, int ServiceId, string Note)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            if (RecordId == 0)
            {
                TempData["Error"] = "Vui lòng lưu bệnh án trước khi chỉ định dịch vụ!";
                return RedirectToAction(nameof(Examine), new { id = AppointmentId });
            }

            var rs = new RecordService
            {
                RecordId = RecordId,
                DoctorId = empId,
                ServiceId = ServiceId,
                ExecuteDate = DateTime.Now,
                Status = "Chờ thực hiện",
                ResultValue = Note
            };

            _context.Add(rs);
            await _context.SaveChangesAsync();

            var record = await _context.MedicalRecords.FindAsync(RecordId);
            var invoice = await GetOrCreateActiveInvoiceAsync(record.PatientId, RecordId);
            var service = await _context.MedicalServices.FindAsync(ServiceId);

            if (service != null)
            {
                var invDetail = new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ItemType = "Dịch vụ",
                    ItemId = service.ServiceId,
                    ItemName = service.ServiceName,
                    Quantity = 1,
                    UnitPrice = service.BasePrice
                };
                _context.InvoiceDetails.Add(invDetail);

                invoice.TotalAmount = (invoice.TotalAmount ?? 0) + service.BasePrice;
                invoice.PatientAmount = invoice.TotalAmount - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;
                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Thêm chỉ định dịch vụ thành công!";
            return RedirectToAction(nameof(Examine), new { id = AppointmentId });
        }
        private async Task<Invoice> GetOrCreateActiveInvoiceAsync(int patientId, int recordId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.RecordId == recordId && i.Status != "DaThanhToan" && i.Status != "Huy");

            if (invoice == null)
            {
                invoice = new Invoice
                {
                    PatientId = patientId,
                    RecordId = recordId,
                    TotalAmount = 0,
                    BhytAmount = 0,
                    DiscountAmount = 0,
                    PatientAmount = 0,
                    Status = "ChuaThanhToan",
                    PaymentMethod = "Tiền mặt",
                    Notes = "Hóa đơn tự động sinh từ Bàn khám"
                };
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
            }
            return invoice;
        }
    }
}
