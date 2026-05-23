using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;
using Microsoft.Extensions.DependencyInjection;
using HTTTBenhVien.Services;

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

            // 0. Tự động hủy các lịch hẹn trễ quá ngày hoặc quá giờ làm việc (sau 21:00)
            var now = DateTime.Now;
            var isPastWorkingHours = now.Hour >= 21; // Sau 21:00
            
            var staleAppointments = await _context.Appointments
                .Where(a => a.AppDate.HasValue && 
                    ((a.AppDate.Value.Date < today) || (a.AppDate.Value.Date == today && isPastWorkingHours))
                    && (a.Status == "Pending" || a.Status == "Chờ xác nhận" || a.Status == "New" || a.Status == "Mới" || a.Status == "Đang chờ" || a.Status == "ChoXacNhan" || a.Status == "Confirmed" || a.Status == "Đã xác nhận" || a.Status == "Xác nhận" || a.Status == "DaXacNhan"))
                .ToListAsync();

            if (staleAppointments.Any())
            {
                foreach (var app in staleAppointments)
                {
                    app.Status = "Cancelled";
                    app.Notes = string.IsNullOrEmpty(app.Notes)
                        ? "Tự động hủy do trễ hẹn qua ngày / quá giờ làm việc"
                        : app.Notes + " | Tự động hủy do trễ hẹn qua ngày / quá giờ làm việc";
                }
                await _context.SaveChangesAsync();
            }

            // 1. Lấy tất cả lịch hẹn hôm nay của bác sĩ này (chỉ hiển thị các lịch hẹn đã xác nhận/tiếp nhận)
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == empId && a.AppDate != null && a.AppDate.Value.Date == today 
                    && a.Status != "Huy" && a.Status != "Cancelled" && a.Status != "ChoXacNhan" && a.Status != "Pending" && a.Status != "Chờ xác nhận")
                .OrderBy(a => a.TimeSlot)
                .ToListAsync();

            // 2. Lấy tất cả bệnh án hôm nay của bác sĩ này (bao gồm cả vãng lai và đã khám)
            var medicalRecords = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.RecordServices)
                    .ThenInclude(rs => rs.Service)
                .Where(m => m.DoctorId == empId && m.VisitDate != null && m.VisitDate.Value.Date == today)
                .ToListAsync();

            // 3. Xây dựng hàng đợi chờ khám thống nhất (Chưa có chuẩn đoán/kết luận lâm sàng)
            var queue = new List<PatientQueueItem>();

            // A. Thêm bệnh nhân từ lịch hẹn
            foreach (var app in appointments)
            {
                var record = medicalRecords.FirstOrDefault(m => m.AppointmentId == app.AppId);
                bool isExamined = record != null && (!string.IsNullOrEmpty(record.Diagnosis) || !string.IsNullOrEmpty(record.Conclusion));

                // Tính toán xem có trễ hẹn không
                bool isLate = false;
                if (!isExamined && app.AppDate.HasValue)
                {
                    if (app.AppDate.Value.Date < today)
                    {
                        isLate = true;
                    }
                    else if (app.AppDate.Value.Date == today && !string.IsNullOrEmpty(app.TimeSlot))
                    {
                        var rawSlot = app.TimeSlot.Split('-')[0].Trim();
                        if (TimeSpan.TryParse(rawSlot, out var slotTime))
                        {
                            if (DateTime.Now.TimeOfDay > slotTime)
                            {
                                isLate = true;
                            }
                        }
                    }
                }

                queue.Add(new PatientQueueItem
                {
                    AppId = app.AppId,
                    RecordId = record?.RecordId,
                    PatientId = app.PatientId,
                    PatientName = app.Patient?.FullName ?? "Bệnh nhân ẩn danh",
                    Phone = app.Patient?.Phone,
                    Gender = app.Patient?.Gender,
                    TimeSlot = app.TimeSlot ?? "Lịch hẹn",
                    Reason = app.Reason,
                    Type = "Lịch hẹn",
                    IsExamined = isExamined,
                    RegisterTime = app.CreatedDate ?? today,
                    IsLate = isLate,
                    Status = app.Status,
                    AppDate = app.AppDate
                });
            }

            // B. Thêm bệnh nhân vãng lai khám trực tiếp (AppointmentId == null)
            var walkIns = medicalRecords.Where(m => m.AppointmentId == null).ToList();
            foreach (var w in walkIns)
            {
                bool isExamined = !string.IsNullOrEmpty(w.Diagnosis) || !string.IsNullOrEmpty(w.Conclusion);

                queue.Add(new PatientQueueItem
                {
                    AppId = null,
                    RecordId = w.RecordId,
                    PatientId = w.PatientId,
                    PatientName = w.Patient?.FullName ?? "Bệnh nhân vãng lai",
                    Phone = w.Patient?.Phone,
                    Gender = w.Patient?.Gender,
                    TimeSlot = "Khám trực tiếp",
                    Reason = w.Notes,
                    Type = "Khám trực tiếp",
                    IsExamined = isExamined,
                    RegisterTime = w.VisitDate ?? today
                });
            }

            // Sắp xếp queue: Những ai chưa khám lên trước, đã khám xếp sau; sau đó xếp theo thời gian đăng ký/TimeSlot
            var sortedQueue = queue
                .OrderBy(q => q.IsExamined)
                .ThenBy(q => q.TimeSlot)
                .ThenBy(q => q.RegisterTime)
                .ToList();

            // 4. Phân hệ Theo dõi Cận lâm sàng hôm nay
            var labRecords = medicalRecords.Where(m => m.RecordServices.Any()).ToList();

            var waitingLab = new List<LabTrackingItem>();
            var completedLab = new List<LabTrackingItem>();

            foreach (var r in labRecords)
            {
                var services = r.RecordServices.ToList();
                bool allDone = services.All(s => s.Status == "HoanThanh" || s.Status == "Hoàn thành" || s.Status == "Completed" || s.Status == "Done");
                
                var servicesSummary = string.Join(", ", services.Select(s => $"{s.Service?.ServiceName} ({s.Status})"));

                var item = new LabTrackingItem
                {
                    RecordId = r.RecordId,
                    AppointmentId = r.AppointmentId,
                    PatientId = r.PatientId,
                    PatientName = r.Patient?.FullName ?? "Bệnh nhân",
                    Phone = r.Patient?.Phone,
                    Gender = r.Patient?.Gender,
                    OrderedServices = servicesSummary,
                    LabStatus = allDone ? "Đã có kết quả" : "Đang chờ kết quả",
                    AllCompleted = allDone
                };

                if (allDone)
                {
                    completedLab.Add(item);
                }
                else
                {
                    waitingLab.Add(item);
                }
            }

            // Thống kê & Dữ liệu
            ViewBag.Queue = sortedQueue;
            ViewBag.WaitingLab = waitingLab;
            ViewBag.CompletedLab = completedLab;
            
            ViewBag.TotalPatients = sortedQueue.Count;
            ViewBag.Examined = sortedQueue.Count(q => q.IsExamined);
            ViewBag.Waiting = sortedQueue.Count(q => !q.IsExamined);

            ViewBag.WaitingLabCount = waitingLab.Count;
            ViewBag.CompletedLabCount = completedLab.Count;

            return View();
        }

        // GET: Admin/DoctorWorkspace/Examine
        public async Task<IActionResult> Examine(int? appId, int? recordId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            MedicalRecord record = null;

            if (recordId.HasValue && recordId.Value > 0)
            {
                record = await _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Include(m => m.Invoice)
                    .Include(m => m.TargetDept)
                    .Include(m => m.Prescriptions)
                        .ThenInclude(p => p.PrescriptionDetails)
                            .ThenInclude(d => d.Medicine)
                    .Include(m => m.RecordServices)
                        .ThenInclude(rs => rs.Service)
                    .FirstOrDefaultAsync(m => m.RecordId == recordId.Value && m.DoctorId == empId);
            }
            else if (appId.HasValue && appId.Value > 0)
            {
                // Tìm xem đã có bệnh án chưa
                record = await _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Include(m => m.Invoice)
                    .Include(m => m.TargetDept)
                    .Include(m => m.Prescriptions)
                        .ThenInclude(p => p.PrescriptionDetails)
                            .ThenInclude(d => d.Medicine)
                    .Include(m => m.RecordServices)
                        .ThenInclude(rs => rs.Service)
                    .FirstOrDefaultAsync(m => m.AppointmentId == appId.Value && m.DoctorId == empId);

                if (record == null)
                {
                    var appointment = await _context.Appointments
                        .Include(a => a.Patient)
                        .FirstOrDefaultAsync(a => a.AppId == appId.Value && a.DoctorId == empId);

                    if (appointment == null) return NotFound();

                    // Tự động tạo và lưu bệnh án mới để có RecordId cho kê đơn/chỉ định
                    record = new MedicalRecord
                    {
                        AppointmentId = appointment.AppId,
                        PatientId = appointment.PatientId,
                        DoctorId = empId,
                        VisitDate = DateTime.Now,
                        RecordType = "Khám ngoại trú",
                        Symptoms = appointment.Reason,
                        Notes = ""
                    };
                    _context.MedicalRecords.Add(record);
                    await _context.SaveChangesAsync();

                    // Tải lại đầy đủ quan hệ
                    record = await _context.MedicalRecords
                        .Include(m => m.Patient)
                        .Include(m => m.Invoice)
                        .Include(m => m.TargetDept)
                        .Include(m => m.Prescriptions)
                        .Include(m => m.RecordServices)
                        .FirstOrDefaultAsync(m => m.RecordId == record.RecordId);
                }
            }

            if (record == null) return NotFound();

            ViewBag.IcdCodes = await _context.IcdDictionaries.Where(i => i.IsActive == true).ToListAsync();
            ViewBag.Medicines = await _context.Medicines.Where(m => m.IsActive != false).OrderBy(m => m.MedicineName).ToListAsync();
            ViewBag.MedicalServices = await _context.MedicalServices.OrderBy(s => s.ServiceName).ToListAsync();
            ViewBag.ClinicalDepartments = await _context.Departments
                .Where(d => d.DeptTypeId == 1 && d.DeptId != 1)
                .OrderBy(d => d.DeptName)
                .ToListAsync();
            return View(record);
        }

        // POST: Admin/DoctorWorkspace/UpdatePatientHistory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePatientHistory(int PatientId, int? RecordId, int? AppointmentId, string? PersonalMedicalHistory, string? FamilyMedicalHistory)
        {
            var patient = await _context.Patients.FindAsync(PatientId);
            if (patient != null)
            {
                patient.PersonalMedicalHistory = PersonalMedicalHistory;
                patient.FamilyMedicalHistory = FamilyMedicalHistory;
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật tiền sử bệnh thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy hồ sơ bệnh nhân.";
            }
            return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
        }

        // POST: Admin/DoctorWorkspace/SaveRecord
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRecord([Bind("RecordId,PatientId,DoctorId,AppointmentId,Symptoms,Diagnosis,Conclusion,RecordType,IcdCode,Notes,BloodPressure,PulseRate,Temperature,Height,Weight,SpO2,TargetDeptId,AdmissionNotes")] MedicalRecord record)
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
                    if (record.RecordType != "Nhập viện")
                    {
                        record.TargetDeptId = null;
                        record.AdmissionNotes = null;
                    }
                    _context.Add(record);
                    
                    if (record.RecordType == "Nhập viện")
                    {
                        var deptName = await _context.Departments
                            .Where(d => d.DeptId == record.TargetDeptId)
                            .Select(d => d.DeptName)
                            .FirstOrDefaultAsync() ?? "Khoa điều trị";
                        TempData["Success"] = $"Đã gửi yêu cầu nhập viện thành công đến {deptName}!";
                    }
                    else
                    {
                        TempData["Success"] = "Đã lưu bệnh án thành công!";
                    }
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
                        existing.RecordType = record.RecordType;
                        
                        if (record.RecordType == "Nhập viện")
                        {
                            existing.TargetDeptId = record.TargetDeptId;
                            existing.AdmissionNotes = record.AdmissionNotes;
                        }
                        else
                        {
                            existing.TargetDeptId = null;
                            existing.AdmissionNotes = null;
                        }
                        
                        _context.Update(existing);
                        
                        if (record.RecordType == "Nhập viện")
                        {
                            var deptName = await _context.Departments
                                .Where(d => d.DeptId == record.TargetDeptId)
                                .Select(d => d.DeptName)
                                .FirstOrDefaultAsync() ?? "Khoa điều trị";
                            TempData["Success"] = $"Đã gửi yêu cầu nhập viện thành công đến {deptName}!";
                        }
                        else
                        {
                            TempData["Success"] = "Đã cập nhật bệnh án thành công!";
                        }
                    }
                }

                // Tự động xác nhận lịch hẹn khi bác sĩ đã khám và lưu bệnh án
                if (record.AppointmentId.HasValue && record.AppointmentId.Value > 0)
                {
                    var appointment = await _context.Appointments.FindAsync(record.AppointmentId.Value);
                    if (appointment != null)
                    {
                        appointment.Status = "CheckedIn";
                        _context.Appointments.Update(appointment);
                    }
                }

                await _context.SaveChangesAsync();

                // Tự động sinh hóa đơn/tiền khám lâm sàng khi bác sĩ lưu bệnh án
                var hasClsServices = await _context.RecordServices.AnyAsync(rs => rs.RecordId == record.RecordId);
                var invoice = await GetOrCreateActiveInvoiceAsync(record.PatientId, record.RecordId);
                
                // Nếu bệnh nhân không có chỉ định cận lâm sàng nào, tự động đổi trạng thái hóa đơn từ ChuaChot sang ChuaThanhToan
                // để quầy thu ngân có thể lập tức nhìn thấy và thu tiền khám.
                if (!hasClsServices && invoice != null && invoice.Status == "ChuaChot")
                {
                    invoice.Status = "ChuaThanhToan";
                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Examine), new { appId = record.AppointmentId, recordId = record.RecordId });
            }

            TempData["Error"] = "Có lỗi xảy ra khi lưu bệnh án.";
            return RedirectToAction(nameof(Examine), new { appId = record.AppointmentId, recordId = record.RecordId });
        }

        // POST: Admin/DoctorWorkspace/AddPrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrescription(int RecordId, int? AppointmentId, int[] MedicineIds, int[] Quantities, string[] Dosages)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            if (RecordId == 0)
            {
                TempData["Error"] = "Vui lòng lưu bệnh án trước khi kê đơn!";
                return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
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
            return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
        }

        // POST: Admin/DoctorWorkspace/EditPrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPrescription(int PrescriptionId, int RecordId, int? AppointmentId, int[] MedicineIds, int[] Quantities, string[] Dosages)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .FirstOrDefaultAsync(p => p.PrescriptionId == PrescriptionId);

            if (prescription == null)
            {
                TempData["Error"] = "Không tìm thấy đơn thuốc để chỉnh sửa.";
                return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
            }

            // Remove existing medicines from the active invoice
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.RecordId == RecordId);

            if (invoice != null)
            {
                decimal totalToDeduct = 0;
                foreach (var detail in prescription.PrescriptionDetails)
                {
                    var invDetail = invoice.InvoiceDetails.FirstOrDefault(id => id.ItemType == "Thuốc" && id.ItemId == detail.MedicineId);
                    if (invDetail != null)
                    {
                        var medPrice = invDetail.UnitPrice ?? 0;
                        var qty = invDetail.Quantity ?? 0;
                        totalToDeduct += medPrice * qty;
                        _context.InvoiceDetails.Remove(invDetail);
                    }
                }

                if (totalToDeduct > 0)
                {
                    invoice.TotalAmount = (invoice.TotalAmount ?? 0) - totalToDeduct;
                }
            }

            // Clear old prescription details
            _context.PrescriptionDetails.RemoveRange(prescription.PrescriptionDetails);
            await _context.SaveChangesAsync();

            // Add new prescription details & update invoice
            if (MedicineIds != null)
            {
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

                            if (invoice != null)
                            {
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
                }

                if (invoice != null && addedTotal > 0)
                {
                    invoice.TotalAmount = (invoice.TotalAmount ?? 0) + addedTotal;
                    invoice.PatientAmount = invoice.TotalAmount - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                    if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;
                    _context.Update(invoice);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật đơn thuốc thành công!";
            return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
        }

        // POST: Admin/DoctorWorkspace/DeletePrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePrescription(int id, int? appId, int recordId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null)
            {
                TempData["Error"] = "Không tìm thấy đơn thuốc.";
                return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
            }

            // Remove from invoice details
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.RecordId == recordId);

            if (invoice != null)
            {
                decimal totalToDeduct = 0;
                foreach (var detail in prescription.PrescriptionDetails)
                {
                    var invDetail = invoice.InvoiceDetails.FirstOrDefault(id => id.ItemType == "Thuốc" && id.ItemId == detail.MedicineId);
                    if (invDetail != null)
                    {
                        var medPrice = invDetail.UnitPrice ?? 0;
                        var qty = invDetail.Quantity ?? 0;
                        totalToDeduct += medPrice * qty;
                        _context.InvoiceDetails.Remove(invDetail);
                    }
                }

                if (totalToDeduct > 0)
                {
                    invoice.TotalAmount = (invoice.TotalAmount ?? 0) - totalToDeduct;
                    if (invoice.TotalAmount < 0) invoice.TotalAmount = 0;
                    invoice.PatientAmount = invoice.TotalAmount - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                    if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;
                    _context.Update(invoice);
                }
            }

            _context.PrescriptionDetails.RemoveRange(prescription.PrescriptionDetails);
            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa đơn thuốc thành công!";
            return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
        }

        // POST: Admin/DoctorWorkspace/AddRecordService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRecordService(int RecordId, int? AppointmentId, int ServiceId, string Note)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            if (RecordId == 0)
            {
                TempData["Error"] = "Vui lòng lưu bệnh án trước khi chỉ định dịch vụ!";
                return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
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
            return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
        }

        private async Task<Invoice> GetOrCreateActiveInvoiceAsync(int patientId, int recordId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.RecordId == recordId);

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
                    Status = "ChuaChot",
                    PaymentMethod = "Tiền mặt",
                    Notes = "Hóa đơn tự động sinh từ Bàn khám"
                };
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
            }
            else if (invoice.Status == "DaThanhToan" || invoice.Status == "Huy")
            {
                // Nếu hóa đơn đã được thanh toán hoặc bị hủy trước đó nhưng bác sĩ lại lưu bệnh án hoặc kê thêm dịch vụ/đơn thuốc mới,
                // chuyển trạng thái về "ChuaChot" để hệ thống tiếp tục cho phép thu thêm phần phát sinh.
                invoice.Status = "ChuaChot";
                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }

            // Kiểm tra xem đã có hóa đơn nào (đã thanh toán hoặc chưa thanh toán, không tính hóa đơn đã hủy)
            // của bệnh án này chứa khoản mục "Tiền khám" hay chưa để tránh tính phí trùng lặp.
            bool hasClinicalExamFee = await _context.InvoiceDetails
                .AnyAsync(id => id.Invoice.RecordId == recordId 
                             && id.Invoice.Status != "Huy" 
                             && id.ItemType == "Tiền khám");

            if (!hasClinicalExamFee)
            {
                var examFeeDetail = new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ItemType = "Tiền khám",
                    ItemId = 0, // Id = 0 đại diện cho tiền khám lâm sàng tự động
                    ItemName = "Tiền khám lâm sàng",
                    Quantity = 1,
                    UnitPrice = 150000
                };
                _context.InvoiceDetails.Add(examFeeDetail);

                invoice.TotalAmount = (invoice.TotalAmount ?? 0) + 150000;
                invoice.PatientAmount = invoice.TotalAmount - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;

                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }

            return invoice;
        }

        // GET: Admin/DoctorWorkspace/PrintRequisition
        public async Task<IActionResult> PrintRequisition(int recordId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.RecordServices)
                    .ThenInclude(rs => rs.Service)
                .FirstOrDefaultAsync(m => m.RecordId == recordId);

            if (record == null) return NotFound();

            // Xác minh quyền sở hữu hoặc quyền admin
            if (record.DoctorId != empId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(record);
        }

        // GET: Admin/DoctorWorkspace/PrintAdmissionSlip
        public async Task<IActionResult> PrintAdmissionSlip(int recordId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Include(m => m.TargetDept)
                .FirstOrDefaultAsync(m => m.RecordId == recordId);

            if (record == null) return NotFound();

            if (record.DoctorId != empId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(record);
        }

        // POST: Admin/DoctorWorkspace/DeleteRecordService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRecordService(int rsId, int? appId, int? recordId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var rs = await _context.RecordServices
                .Include(r => r.Service)
                .FirstOrDefaultAsync(r => r.RsId == rsId);

            if (rs == null)
            {
                TempData["Error"] = "Không tìm thấy chỉ định cận lâm sàng.";
                return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
            }

            // 1. Kiểm tra xem chỉ định đã được thực hiện chưa
            if (rs.Status != "Chờ thực hiện" && rs.Status != "ChoThucHien")
            {
                TempData["Error"] = "Chỉ định này đã hoặc đang được thực hiện, không thể xóa!";
                return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
            }

            // 2. Kiểm tra xem đã thanh toán chưa
            var isPaid = await _context.InvoiceDetails
                .Include(id => id.Invoice)
                .AnyAsync(id => id.Invoice.RecordId == rs.RecordId 
                             && id.ItemType == "Dịch vụ" 
                             && id.ItemId == rs.ServiceId 
                             && (id.Invoice.Status == "DaThanhToan" 
                              || id.Invoice.Status == "Đã thanh toán" 
                              || id.Invoice.Status == "Paid" 
                              || id.Invoice.Status == "Hoàn thành" 
                              || id.Invoice.Status == "Completed"));

            if (isPaid)
            {
                TempData["Error"] = "Chỉ định này đã được thanh toán tiền, không thể tự ý xóa! Vui lòng liên hệ quầy Thu ngân để làm thủ tục hoàn trả nếu cần.";
                return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
            }

            // 3. Tiến hành xóa chi tiết trong hóa đơn (nếu có hóa đơn chưa thanh toán)
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.RecordId == rs.RecordId 
                             && i.Status != "DaThanhToan" 
                             && i.Status != "Đã thanh toán" 
                             && i.Status != "Paid" 
                             && i.Status != "Hoàn thành" 
                             && i.Status != "Completed" 
                             && i.Status != "Huy");

            if (invoice != null)
            {
                var detail = invoice.InvoiceDetails.FirstOrDefault(d => d.ItemType == "Dịch vụ" && d.ItemId == rs.ServiceId);
                if (detail != null)
                {
                    _context.InvoiceDetails.Remove(detail);
                    
                    // Trừ số tiền tương ứng
                    var servicePrice = detail.UnitPrice * detail.Quantity ?? 0;
                    invoice.TotalAmount -= servicePrice;
                    invoice.PatientAmount = invoice.TotalAmount - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                    if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;
                    
                    _context.Update(invoice);
                }
            }

            // 4. Xóa RecordService khỏi database
            _context.RecordServices.Remove(rs);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa chỉ định cận lâm sàng thành công!";
            return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
        }

        // POST: Admin/DoctorWorkspace/EditRecordService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRecordService(int rsId, int? appId, int? recordId, int ServiceId, string Note)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var rs = await _context.RecordServices
                .Include(r => r.Service)
                .FirstOrDefaultAsync(r => r.RsId == rsId);

            if (rs == null)
            {
                TempData["Error"] = "Không tìm thấy chỉ định cận lâm sàng.";
                return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
            }

            if (rs.Status != "Chờ thực hiện" && rs.Status != "ChoThucHien")
            {
                TempData["Error"] = "Chỉ định này đã hoặc đang được thực hiện, không thể sửa!";
                return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
            }

            // Kiểm tra xem đã thanh toán chưa
            var isPaid = await _context.InvoiceDetails
                .Include(id => id.Invoice)
                .AnyAsync(id => id.Invoice.RecordId == rs.RecordId 
                             && id.ItemType == "Dịch vụ" 
                             && id.ItemId == rs.ServiceId 
                             && (id.Invoice.Status == "DaThanhToan" 
                              || id.Invoice.Status == "Đã thanh toán" 
                              || id.Invoice.Status == "Paid" 
                              || id.Invoice.Status == "Hoàn thành" 
                              || id.Invoice.Status == "Completed"));

            if (isPaid)
            {
                TempData["Error"] = "Chỉ định này đã được thanh toán tiền, không thể sửa! Vui lòng liên hệ quầy Thu ngân.";
                return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
            }

            // Nếu thay đổi dịch vụ cận lâm sàng
            if (rs.ServiceId != ServiceId)
            {
                var oldService = await _context.MedicalServices.FindAsync(rs.ServiceId);
                var newService = await _context.MedicalServices.FindAsync(ServiceId);
                if (newService != null)
                {
                    var invoice = await _context.Invoices
                        .Include(i => i.InvoiceDetails)
                        .FirstOrDefaultAsync(i => i.RecordId == rs.RecordId 
                                     && i.Status != "DaThanhToan" 
                                     && i.Status != "Đã thanh toán" 
                                     && i.Status != "Paid" 
                                     && i.Status != "Hoàn thành" 
                                     && i.Status != "Completed" 
                                     && i.Status != "Huy");

                    if (invoice != null)
                    {
                        var detail = invoice.InvoiceDetails.FirstOrDefault(d => d.ItemType == "Dịch vụ" && d.ItemId == rs.ServiceId);
                        if (detail != null)
                        {
                            detail.ItemId = newService.ServiceId;
                            detail.ItemName = newService.ServiceName;
                            detail.UnitPrice = newService.BasePrice;

                            var priceDiff = newService.BasePrice - (oldService?.BasePrice ?? 0);
                            invoice.TotalAmount += priceDiff;
                            invoice.PatientAmount = invoice.TotalAmount - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                            if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;

                            _context.Update(detail);
                            _context.Update(invoice);
                        }
                    }
                    rs.ServiceId = ServiceId;
                }
            }

            rs.ResultValue = Note;
            _context.Update(rs);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật chỉ định cận lâm sàng thành công!";
            return RedirectToAction(nameof(Examine), new { appId = appId, recordId = recordId });
        }

        // POST: Admin/DoctorWorkspace/ConfirmRecordServices
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRecordServices(int RecordId, int? AppointmentId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var record = await _context.MedicalRecords
                .Include(m => m.RecordServices)
                .FirstOrDefaultAsync(m => m.RecordId == RecordId && m.DoctorId == empId);

            if (record == null) return NotFound();

            if (!record.RecordServices.Any())
            {
                TempData["Error"] = "Chưa có chỉ định cận lâm sàng nào để chốt!";
                return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.RecordId == RecordId && i.Status == "ChuaChot");

            if (invoice != null)
            {
                invoice.Status = "ChuaThanhToan";
                _context.Update(invoice);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã chốt chỉ định cận lâm sàng thành công! Hóa đơn đã được gửi sang quầy thu ngân.";
            }
            else
            {
                TempData["Info"] = "Chỉ định cận lâm sàng đã được chốt trước đó.";
            }

            return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
        }

        // POST: Admin/DoctorWorkspace/UnlockRecordServices
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockRecordServices(int RecordId, int? AppointmentId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var record = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.RecordId == RecordId && m.DoctorId == empId);

            if (record == null) return NotFound();

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.RecordId == RecordId && (i.Status == "ChuaThanhToan" || i.Status == "Chưa thanh toán" || i.Status == "Pending" || i.Status == "Mới" || i.Status == "New" || i.Status == "Unpaid"));

            if (invoice != null)
            {
                invoice.Status = "ChuaChot";
                _context.Update(invoice);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã mở khóa chỉ định thành công! Bạn có thể chỉnh sửa hoặc xóa chỉ định cận lâm sàng ngay bây giờ.";
            }
            else
            {
                var isPaid = await _context.Invoices.AnyAsync(i => i.RecordId == RecordId && (i.Status == "DaThanhToan" || i.Status == "Đã thanh toán" || i.Status == "Paid" || i.Status == "Hoàn thành" || i.Status == "Completed"));
                if (isPaid)
                {
                    TempData["Error"] = "Hóa đơn này đã thanh toán tiền, không thể mở khóa chỉ định!";
                }
                else
                {
                    TempData["Info"] = "Chỉ định đang ở trạng thái Nháp hoặc không thể mở khóa chỉ định.";
                }
            }

            return RedirectToAction(nameof(Examine), new { appId = AppointmentId, recordId = RecordId });
        }

        // POST: Admin/DoctorWorkspace/CancelAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id, string reason = "Bác sĩ hủy lịch do trễ hẹn")
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                // Verify this appointment belongs to this doctor
                if (appointment.DoctorId != empId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                appointment.Status = "Cancelled";
                appointment.Notes = string.IsNullOrEmpty(appointment.Notes) 
                    ? reason 
                    : appointment.Notes + " | " + reason;
                
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã hủy lịch hẹn của bệnh nhân do trễ hẹn thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy lịch hẹn.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // Dịch vụ Thanh toán tại bàn khám (VietQR & VNPay)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
            {
                return Json(new { isPaid = false, message = "Không tìm thấy hóa đơn" });
            }

            var isPaid = invoice.Status == "DaThanhToan" || invoice.Status == "Đã thanh toán" || invoice.Status == "Paid" || invoice.Status == "Completed" || invoice.Status == "Hoàn thành";
            return Json(new { isPaid = isPaid, method = invoice.PaymentMethod });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBankTransferPayment(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hóa đơn" });
            }

            if (invoice.Status == "DaThanhToan" || invoice.Status == "Đã thanh toán" || invoice.Status == "Paid" || invoice.Status == "Completed" || invoice.Status == "Hoàn thành")
            {
                return Json(new { success = true, message = "Hóa đơn đã được thanh toán từ trước." });
            }

            invoice.Status = "DaThanhToan";
            invoice.PaymentDate = DateTime.Now;
            invoice.PaymentMethod = "Chuyển khoản";
            invoice.Notes = "Bác sĩ xác nhận chuyển khoản ngân hàng tại phòng khám.";

            if (!invoice.PatientAmount.HasValue || invoice.PatientAmount == 0)
            {
                var bhyt = invoice.BhytAmount ?? 0;
                var discount = invoice.DiscountAmount ?? 0;
                var total = invoice.TotalAmount ?? 0;
                var finalAmount = total - bhyt - discount;
                invoice.PatientAmount = finalAmount > 0 ? finalAmount : 0;
            }

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xác nhận thanh toán chuyển khoản thành công!" });
        }

        [HttpGet]
        public async Task<IActionResult> GenerateVnpayQrCode(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hóa đơn" });
            }

            if (invoice.Status == "DaThanhToan" || invoice.Status == "Đã thanh toán" || invoice.Status == "Paid" || invoice.Status == "Completed" || invoice.Status == "Hoàn thành")
            {
                return Json(new { success = false, message = "Hóa đơn này đã được thanh toán." });
            }

            if (!invoice.PatientAmount.HasValue || invoice.PatientAmount == 0)
            {
                var bhyt = invoice.BhytAmount ?? 0;
                var discount = invoice.DiscountAmount ?? 0;
                var total = invoice.TotalAmount ?? 0;
                var finalAmount = total - bhyt - discount;
                invoice.PatientAmount = finalAmount > 0 ? finalAmount : 0;
            }

            var vnPayService = HttpContext.RequestServices.GetRequiredService<IVnPayService>();

            var paymentInfo = new PaymentInformationModel
            {
                OrderType = "billpayment",
                Amount = (double)(invoice.PatientAmount ?? 0),
                OrderDescription = $"Thanh toan vien phi hoa don #{invoiceId}",
                Name = invoice.Patient?.FullName ?? "Benh nhan",
                InvoiceId = invoiceId
            };

            var paymentUrl = vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);

            return Json(new { success = true, paymentUrl = paymentUrl });
        }
    }

    // Helper ViewModels cho hàng đợi khám và theo dõi cận lâm sàng
    public class PatientQueueItem
    {
        public int? AppId { get; set; }
        public int? RecordId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string Phone { get; set; }
        public bool? Gender { get; set; }
        public string TimeSlot { get; set; }
        public string Reason { get; set; }
        public string Type { get; set; } // "Lịch hẹn" or "Khám trực tiếp"
        public bool IsExamined { get; set; }
        public DateTime RegisterTime { get; set; }
        public bool IsLate { get; set; }
        public string? Status { get; set; }
        public DateTime? AppDate { get; set; }
    }

    public class LabTrackingItem
    {
        public int RecordId { get; set; }
        public int? AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string Phone { get; set; }
        public bool? Gender { get; set; }
        public string OrderedServices { get; set; }
        public string LabStatus { get; set; } // "Đang chờ kết quả" or "Đã có kết quả"
        public bool AllCompleted { get; set; }
    }
}
