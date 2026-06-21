using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,CapCuu,Cấp cứu,Emergency")]
    public class EmergencyWorkspaceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public EmergencyWorkspaceController(HtttbenhvienContext context)
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

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> Index()
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var today = DateTime.Today;


            var activeRecords = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Invoice)
                .Include(m => m.Doctor)
                .Include(m => m.RecordServices).ThenInclude(rs => rs.Service)
                .Where(m => m.IsEmergency == true && m.Invoice.Status == "CapCuu_ChuaThanhToan")
                .OrderBy(m => m.TriageLevel)
                .ThenBy(m => m.TriageTime)
                .ToListAsync();



            ViewBag.ActiveEmergencyCount = activeRecords.Count;


            double avgWaitTime = 0;
            var timedRecords = activeRecords.Where(m => m.TriageTime.HasValue).ToList();
            if (timedRecords.Any())
            {
                avgWaitTime = timedRecords.Average(m => (DateTime.Now - m.TriageTime.Value).TotalMinutes);
            }
            ViewBag.AvgWaitingTime = Math.Round(avgWaitTime, 1);


            var completedTodayCount = await _context.MedicalRecords
                .Include(m => m.Invoice)
                .Where(m => m.IsEmergency == true 
                            && m.VisitDate >= today 
                            && m.Invoice.Status != "CapCuu_ChuaThanhToan"
                            && (m.RecordType == "Xuất viện" || m.RecordType == "Khám ngoại trú" || string.IsNullOrEmpty(m.RecordType)))
                .CountAsync();
            ViewBag.CompletedTodayCount = completedTodayCount;


            var transferredTodayCount = await _context.MedicalRecords
                .Include(m => m.Invoice)
                .Where(m => m.IsEmergency == true 
                            && m.VisitDate >= today 
                            && m.Invoice.Status != "CapCuu_ChuaThanhToan"
                            && (m.RecordType == "Nhập viện" || m.RecordType == "Chuyển viện"))
                .CountAsync();
            ViewBag.TransferredTodayCount = transferredTodayCount;


            var doctors = await _context.Employees
                .Where(e => e.JobTitle.Contains("Bác sĩ") || e.Specialty.Contains("Cấp cứu") || (e.Dept != null && e.Dept.DeptName.Contains("Cấp cứu")))
                .ToListAsync();
            
            if (!doctors.Any())
            {
                doctors = await _context.Employees.Take(20).ToListAsync();
            }
            ViewBag.Doctors = doctors;


            var triage1 = activeRecords.Where(r => r.TriageLevel == 1).ToList(); 
            var triage2 = activeRecords.Where(r => r.TriageLevel == 2).ToList(); 
            var triage3 = activeRecords.Where(r => r.TriageLevel == 3).ToList(); 
            var triage4 = activeRecords.Where(r => r.TriageLevel == 4).ToList(); 
            var triage5 = activeRecords.Where(r => r.TriageLevel == 5).ToList(); 

            ViewBag.Triage1 = triage1;
            ViewBag.Triage2 = triage2;
            ViewBag.Triage3 = triage3;
            ViewBag.Triage4 = triage4;
            ViewBag.Triage5 = triage5;


            ViewBag.Departments = await _context.Departments
                .Where(d => d.DeptName != "Khoa Cấp cứu" && !d.DeptName.Contains("Hành chính"))
                .ToListAsync();

            return View();
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Triage(
            bool IsAnonymous, string? FullName, bool Gender, int? Age, 
            string? Phone, string? Address, int TriageLevel, string? TriageNotes, 
            int DoctorId, string? BloodPressure, int? PulseRate, 
            decimal? Temperature, int? Height, decimal? Weight, int? SpO2)
        {
            Patient patient = null;

            if (IsAnonymous)
            {

                var codePrefix = "CC-" + DateTime.Today.ToString("yyyyMMdd") + "-";
                var lastPatient = await _context.Patients
                    .Where(p => p.PatientCode.StartsWith(codePrefix))
                    .OrderByDescending(p => p.PatientCode)
                    .FirstOrDefaultAsync();

                int nextNum = 1;
                if (lastPatient != null && lastPatient.PatientCode.Length > codePrefix.Length)
                {
                    if (int.TryParse(lastPatient.PatientCode.Substring(codePrefix.Length), out int num))
                    {
                        nextNum = num + 1;
                    }
                }

                var patientCode = codePrefix + nextNum.ToString("D3");
                patient = new Patient
                {
                    PatientCode = patientCode,
                    FullName = string.IsNullOrEmpty(FullName) ? ("BN CẤP CỨU " + patientCode) : FullName,
                    Gender = Gender,
                    Dob = Age.HasValue ? DateTime.Today.AddYears(-Age.Value) : DateTime.Today.AddYears(-30),
                    Phone = Phone,
                    Address = Address ?? "Không rõ",
                    Notes = "Bệnh nhân cấp cứu - chưa xác định danh tính."
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }
            else
            {

                if (!string.IsNullOrEmpty(Phone))
                {
                    patient = await _context.Patients.FirstOrDefaultAsync(p => p.Phone == Phone);
                }

                if (patient == null)
                {
                    var codePrefix = "BN" + DateTime.Today.ToString("yyyyMMdd") + "-";
                    var lastPatient = await _context.Patients
                        .Where(p => p.PatientCode.StartsWith(codePrefix))
                        .OrderByDescending(p => p.PatientCode)
                        .FirstOrDefaultAsync();

                    int nextNum = 1;
                    if (lastPatient != null && lastPatient.PatientCode.Length > codePrefix.Length)
                    {
                        if (int.TryParse(lastPatient.PatientCode.Substring(codePrefix.Length), out int num))
                        {
                            nextNum = num + 1;
                        }
                    }

                    var patientCode = codePrefix + nextNum.ToString("D3");
                    patient = new Patient
                    {
                        PatientCode = patientCode,
                        FullName = FullName ?? "Bệnh nhân Cấp cứu",
                        Gender = Gender,
                        Dob = Age.HasValue ? DateTime.Today.AddYears(-Age.Value) : DateTime.Today.AddYears(-30),
                        Phone = Phone,
                        Address = Address ?? "Chưa rõ",
                        Notes = "Bệnh nhân cấp cứu."
                    };

                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }
            }


            var timestamp = DateTime.Now;
            var record = new MedicalRecord
            {
                PatientId = patient.PatientId,
                DoctorId = DoctorId > 0 ? DoctorId : GetCurrentEmpId(),
                VisitDate = timestamp,
                RecordType = "Cấp cứu",
                Symptoms = TriageNotes ?? "Chấn thương / Cấp cứu khẩn cấp",
                IsEmergency = true,
                TriageLevel = TriageLevel,
                TriageTime = timestamp,
                TriageNotes = TriageNotes,
                BloodPressure = BloodPressure,
                PulseRate = PulseRate,
                Temperature = Temperature,
                Height = Height,
                Weight = Weight,
                SpO2 = SpO2
            };


            var timeStr = timestamp.ToString("yyyy-MM-ddTHH:mm:ss");
            record.VitalSigns = $"{BloodPressure ?? ""}|{PulseRate?.ToString() ?? ""}|{Temperature?.ToString() ?? ""}|{SpO2?.ToString() ?? ""}|{timeStr}";

            _context.MedicalRecords.Add(record);
            await _context.SaveChangesAsync();


            var invoice = new Invoice
            {
                PatientId = patient.PatientId,
                RecordId = record.RecordId,
                TotalAmount = 150000, 
                BhytAmount = 0,
                DiscountAmount = 0,
                PatientAmount = 150000,
                Status = "CapCuu_ChuaThanhToan",
                PaymentMethod = "Chuyển khoản",
                Notes = "Hóa đơn ghi nợ cấp cứu ban đầu"
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();


            var examFee = new InvoiceDetail
            {
                InvoiceId = invoice.InvoiceId,
                ItemType = "Tiền khám",
                ItemId = 0,
                ItemName = "Tiền khám lâm sàng cấp cứu",
                Quantity = 1,
                UnitPrice = 150000
            };

            _context.InvoiceDetails.Add(examFee);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tiếp nhận bệnh nhân cấp cứu thành công. Mã HS: #{record.RecordId}!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTriage(int recordId, int triageLevel, string? notes)
        {
            var record = await _context.MedicalRecords.FindAsync(recordId);
            if (record != null)
            {
                record.TriageLevel = triageLevel;
                if (!string.IsNullOrEmpty(notes))
                {
                    record.TriageNotes = (string.IsNullOrEmpty(record.TriageNotes) ? "" : record.TriageNotes + " | ") + notes;
                }
                _context.Update(record);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật mức Triage thành công cho HS #{recordId}!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy hồ sơ bệnh án.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> Examine(int recordId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Invoice).ThenInclude(i => i.InvoiceDetails)
                .Include(m => m.Doctor)
                .Include(m => m.Prescriptions).ThenInclude(p => p.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
                .Include(m => m.RecordServices).ThenInclude(rs => rs.Service)
                .FirstOrDefaultAsync(m => m.RecordId == recordId);

            if (record == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ bệnh án cấp cứu.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.IcdCodes = await _context.IcdDictionaries.Where(i => i.IsActive == true).ToListAsync();
            ViewBag.Medicines = await _context.Medicines.Where(m => m.IsActive != false).OrderBy(m => m.MedicineName).ToListAsync();
            ViewBag.MedicalServices = await _context.MedicalServices.OrderBy(s => s.ServiceName).ToListAsync();
            

            ViewBag.Departments = await _context.Departments
                .Where(d => d.DeptName != "Khoa Cấp cứu" && !d.DeptName.Contains("Hành chính"))
                .ToListAsync();

            return View(record);
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRecord(
            int RecordId, string? Symptoms, string? Diagnosis, string? Conclusion, 
            string? IcdCode, string? Notes, string? BloodPressure, int? PulseRate, 
            decimal? Temperature, int? Height, decimal? Weight, int? SpO2)
        {
            var existing = await _context.MedicalRecords.FindAsync(RecordId);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy bệnh án.";
                return RedirectToAction(nameof(Index));
            }

            existing.Symptoms = Symptoms;
            existing.Diagnosis = Diagnosis;
            existing.Conclusion = Conclusion;
            existing.IcdCode = IcdCode;
            existing.Notes = Notes;


            existing.BloodPressure = BloodPressure;
            existing.PulseRate = PulseRate;
            existing.Temperature = Temperature;
            existing.Height = Height;
            existing.Weight = Weight;
            existing.SpO2 = SpO2;


            if (!string.IsNullOrEmpty(BloodPressure) || PulseRate.HasValue || Temperature.HasValue || SpO2.HasValue)
            {
                var timeStr = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                var entry = $"{BloodPressure ?? ""}|{PulseRate?.ToString() ?? ""}|{Temperature?.ToString() ?? ""}|{SpO2?.ToString() ?? ""}|{timeStr}";
                
                if (string.IsNullOrEmpty(existing.VitalSigns))
                {
                    existing.VitalSigns = entry;
                }
                else
                {
                    existing.VitalSigns += ";" + entry;
                }
            }

            _context.Update(existing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin khám cấp cứu!";
            return RedirectToAction(nameof(Examine), new { recordId = RecordId });
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmergencyService(int RecordId, int ServiceId, string? Note)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var record = await _context.MedicalRecords.FindAsync(RecordId);
            if (record == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ.";
                return RedirectToAction(nameof(Index));
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

            _context.RecordServices.Add(rs);
            await _context.SaveChangesAsync();


            var invoice = await GetOrCreateEmergencyInvoiceAsync(record.PatientId, RecordId);
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

                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Chỉ định dịch vụ cận lâm sàng khẩn cấp thành công!";
            return RedirectToAction(nameof(Examine), new { recordId = RecordId });
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrescription(int RecordId, int[] MedicineIds, int[] Quantities, string[] Dosages)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var record = await _context.MedicalRecords.FindAsync(RecordId);
            if (record == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ.";
                return RedirectToAction(nameof(Index));
            }

            var prescription = new Prescription
            {
                RecordId = RecordId,
                DoctorId = empId,
                CreateDate = DateTime.Now,
                Status = "ChoCapPhat",
                Note = "Kê thuốc cấp cứu khẩn"
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            if (MedicineIds != null && MedicineIds.Length > 0)
            {
                var invoice = await GetOrCreateEmergencyInvoiceAsync(record.PatientId, RecordId);
                var medicines = await _context.Medicines.Where(m => MedicineIds.Contains(m.MedicineId)).ToListAsync();
                decimal addedTotal = 0;

                for (int i = 0; i < MedicineIds.Length; i++)
                {
                    if (MedicineIds[i] > 0)
                    {
                        var quantity = i < Quantities.Length ? Quantities[i] : 1;
                        var dosage = i < Dosages.Length ? Dosages[i] : "";

                        var detail = new PrescriptionDetail
                        {
                            PrescriptionId = prescription.PrescriptionId,
                            MedicineId = MedicineIds[i],
                            Quantity = quantity,
                            Dosage = dosage
                        };
                        _context.PrescriptionDetails.Add(detail);

                        var med = medicines.FirstOrDefault(m => m.MedicineId == MedicineIds[i]);
                        if (med != null)
                        {
                            var lineTotal = quantity * med.Price;
                            addedTotal += lineTotal;

                            var invDetail = new InvoiceDetail
                            {
                                InvoiceId = invoice.InvoiceId,
                                ItemType = "Thuốc",
                                ItemId = med.MedicineId,
                                ItemName = med.MedicineName,
                                Quantity = quantity,
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

                    _context.Invoices.Update(invoice);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Đã kê đơn thuốc cấp cứu khẩn cấp!";
            return RedirectToAction(nameof(Examine), new { recordId = RecordId });
        }

        // POST: Admin/EmergencyWorkspace/EditPrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPrescription(int PrescriptionId, int RecordId, int[] MedicineIds, int[] Quantities, string[] Dosages)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .FirstOrDefaultAsync(p => p.PrescriptionId == PrescriptionId);

            if (prescription == null)
            {
                TempData["Error"] = "Không tìm thấy đơn thuốc.";
                return RedirectToAction(nameof(Examine), new { recordId = RecordId });
            }

            // Remove existing medicines from the emergency invoice
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
            if (MedicineIds != null && MedicineIds.Length > 0)
            {
                var medicines = await _context.Medicines.Where(m => MedicineIds.Contains(m.MedicineId)).ToListAsync();
                decimal addedTotal = 0;

                for (int i = 0; i < MedicineIds.Length; i++)
                {
                    if (MedicineIds[i] > 0)
                    {
                        var quantity = i < Quantities.Length ? Quantities[i] : 1;
                        var dosage = i < Dosages.Length ? Dosages[i] : "";

                        var detail = new PrescriptionDetail
                        {
                            PrescriptionId = prescription.PrescriptionId,
                            MedicineId = MedicineIds[i],
                            Quantity = quantity,
                            Dosage = dosage
                        };
                        _context.PrescriptionDetails.Add(detail);

                        var med = medicines.FirstOrDefault(m => m.MedicineId == MedicineIds[i]);
                        if (med != null)
                        {
                            var lineTotal = quantity * med.Price;
                            addedTotal += lineTotal;

                            if (invoice != null)
                            {
                                var invDetail = new InvoiceDetail
                                {
                                    InvoiceId = invoice.InvoiceId,
                                    ItemType = "Thuốc",
                                    ItemId = med.MedicineId,
                                    ItemName = med.MedicineName,
                                    Quantity = quantity,
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
                    _context.Invoices.Update(invoice);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật đơn thuốc thành công!";
            return RedirectToAction(nameof(Examine), new { recordId = RecordId });
        }

        // POST: Admin/EmergencyWorkspace/DeletePrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePrescription(int id, int recordId)
        {
            int empId = GetCurrentEmpId();
            if (empId == 0) return RedirectToAction("Login", "Account");

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null)
            {
                TempData["Error"] = "Không tìm thấy đơn thuốc.";
                return RedirectToAction(nameof(Examine), new { recordId = recordId });
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
                    _context.Invoices.Update(invoice);
                }
            }

            _context.PrescriptionDetails.RemoveRange(prescription.PrescriptionDetails);
            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa đơn thuốc thành công!";
            return RedirectToAction(nameof(Examine), new { recordId = recordId });
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(int recordId, string transferType, int? targetDeptId, string? targetHospital, string? transferReason)
        {
            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.RecordId == recordId);

            if (record == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ bệnh án.";
                return RedirectToAction(nameof(Index));
            }

            var noteMsg = "";
            if (transferType == "ChuyenKhoa")
            {
                var deptName = "khoa lâm sàng";
                if (targetDeptId.HasValue)
                {
                    var dept = await _context.Departments.FindAsync(targetDeptId.Value);
                    if (dept != null) deptName = dept.DeptName;
                }
                record.RecordType = "Nhập viện";
                record.Conclusion = $"Chuyển viện nội bộ - Nhập viện khoa: {deptName}";
                noteMsg = $"Đã chuyển bệnh nhân #{recordId} nhập viện tại {deptName}. Lý do: {transferReason ?? "Cấp cứu ổn định"}";
            }
            else // ChuyenVien
            {
                record.RecordType = "Chuyển viện";
                record.Conclusion = $"Chuyển viện tuyến trên: {targetHospital ?? "Bệnh viện đa khoa tỉnh"}";
                noteMsg = $"Đã chuyển bệnh nhân #{recordId} tới {targetHospital ?? "tuyến trên"}. Lý do: {transferReason ?? "Vượt quá khả năng chuyên môn"}";
            }

            record.Notes = (string.IsNullOrEmpty(record.Notes) ? "" : record.Notes + " | ") + noteMsg;
            _context.Update(record);
            await _context.SaveChangesAsync();


            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.RecordId == recordId && i.Status == "CapCuu_ChuaThanhToan");
            if (invoice != null)
            {
                invoice.Status = "ChuaThanhToan";
                invoice.Notes += $" | Chốt hóa đơn do {record.RecordType}";
                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = noteMsg;
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePatientInfo(
            int patientId, int recordId, string fullName, DateTime? dob, 
            bool gender, string? phone, string? address, string? citizenId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                TempData["Error"] = "Không tìm thấy bệnh nhân.";
                return RedirectToAction(nameof(Examine), new { recordId = recordId });
            }

            patient.FullName = fullName;
            if (dob.HasValue) patient.Dob = dob.Value;
            patient.Gender = gender;
            patient.Phone = phone;
            patient.Address = address;
            patient.CitizenId = citizenId;
            patient.Notes = "Đã xác thực danh tính sau cấp cứu.";

            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin hành chính bệnh nhân!";
            return RedirectToAction(nameof(Examine), new { recordId = recordId });
        }

        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Discharge(int recordId, string conclusion, string dischargeNotes)
        {
            var record = await _context.MedicalRecords.FindAsync(recordId);
            if (record == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ.";
                return RedirectToAction(nameof(Index));
            }

            record.RecordType = "Xuất viện";
            record.Conclusion = conclusion;
            record.Notes = (string.IsNullOrEmpty(record.Notes) ? "" : record.Notes + " | ") + $"Xuất viện: {dischargeNotes}";

            _context.Update(record);
            await _context.SaveChangesAsync();


            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.RecordId == recordId && i.Status == "CapCuu_ChuaThanhToan");
            if (invoice != null)
            {
                invoice.Status = "ChuaThanhToan";
                invoice.Notes += " | Chốt hóa đơn xuất viện cấp cứu";
                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Bệnh nhân #{recordId} đã hoàn tất cấp cứu và xuất viện!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════
        // GET VITAL SIGNS HISTORY FOR CHART (JSON)
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetVitalSignsHistory(int recordId)
        {
            var record = await _context.MedicalRecords.FindAsync(recordId);
            if (record == null || string.IsNullOrEmpty(record.VitalSigns))
            {
                return Json(new List<object>());
            }

            var entries = record.VitalSigns.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var historyList = new List<object>();

            foreach (var entry in entries)
            {
                var parts = entry.Split('|');
                if (parts.Length >= 5)
                {
                    historyList.Add(new
                    {
                        BloodPressure = parts[0],
                        PulseRate = parts[1],
                        Temperature = parts[2],
                        SpO2 = parts[3],
                        Time = parts[4]
                    });
                }
            }

            return Json(historyList);
        }

        // ═══════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════
        private async Task<Invoice> GetOrCreateEmergencyInvoiceAsync(int patientId, int recordId)
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
                    Status = "CapCuu_ChuaThanhToan",
                    PaymentMethod = "Chuyển khoản",
                    Notes = "Hóa đơn nợ khoa Cấp cứu"
                };
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
            }
            else if (invoice.Status != "CapCuu_ChuaThanhToan" && invoice.Status != "DaThanhToan" && invoice.Status != "Huy")
            {

                invoice.Status = "CapCuu_ChuaThanhToan";
                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }
            else if (invoice.Status == "DaThanhToan")
            {


                invoice.Status = "CapCuu_ChuaThanhToan";
                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }


            bool hasClinicalExamFee = await _context.InvoiceDetails
                .AnyAsync(id => id.Invoice.RecordId == recordId && id.ItemType == "Tiền khám");

            if (!hasClinicalExamFee)
            {
                var examFeeDetail = new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ItemType = "Tiền khám",
                    ItemId = 0,
                    ItemName = "Tiền khám lâm sàng cấp cứu",
                    Quantity = 1,
                    UnitPrice = 150000
                };
                _context.InvoiceDetails.Add(examFeeDetail);

                invoice.TotalAmount = (invoice.TotalAmount ?? 0) + 150000;
                invoice.PatientAmount = invoice.TotalAmount - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
                if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;

                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
            }

            return invoice;
        }
    }
}
