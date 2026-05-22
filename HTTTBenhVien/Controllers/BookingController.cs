using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HTTTBenhVien.Models;
using Microsoft.Data.SqlClient;


namespace HTTTBenhVien.Controllers;

[Authorize(Roles = "Patient")]
public class BookingController : Controller
{
    private readonly HtttbenhvienContext _context;

    public BookingController(HtttbenhvienContext context)
    {
        _context = context;
    }

    private async Task PopulateViewBagsAsync(int patientId)
    {
        var departments = await _context.Departments
            .Include(d => d.DeptTypeNavigation)
            .Where(d => d.DeptTypeNavigation != null && d.DeptTypeNavigation.DeptTypeName == "Lâm sàng")
            .OrderBy(d => d.DeptName)
            .ToListAsync();

        var doctors = await _context.Employees
            .Include(e => e.Dept)
            .Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ"))
            .OrderBy(e => e.FullName)
            .ToListAsync();

        var relatives = await _context.Patients
            .Where(p => p.CreatedByPatientId == patientId)
            .OrderBy(p => p.FullName)
            .ToListAsync();

        ViewBag.Departments = departments;
        ViewBag.Doctors = doctors;
        ViewBag.Relatives = relatives;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var patientId = int.Parse(User.FindFirstValue("PatientId") ?? "0");
        await PopulateViewBagsAsync(patientId);

        var currentPatient = await _context.Patients.FindAsync(patientId);

        var model = new Appointment
        {
            Patient = currentPatient ?? new Patient()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Appointment model)
    {
        var loggedInPatientId = int.Parse(User.FindFirstValue("PatientId") ?? "0");

        if (!ModelState.IsValid)
        {
            await PopulateViewBagsAsync(loggedInPatientId);
            return View(model);
        }

        if (model.AppDate.HasValue)
        {
            var bookingDate = model.AppDate.Value.Date;
            if (bookingDate < DateTime.Today)
            {
                ModelState.AddModelError("", "Không thể đặt lịch hẹn cho ngày đã qua. Vui lòng chọn ngày khám từ hôm nay trở đi.");
                await PopulateViewBagsAsync(loggedInPatientId);
                return View(model);
            }

            if (bookingDate == DateTime.Today && !string.IsNullOrEmpty(model.TimeSlot))
            {
                if (TimeSpan.TryParse(model.TimeSlot, out var slotTime))
                {
                    if (DateTime.Now.TimeOfDay >= slotTime)
                    {
                        ModelState.AddModelError("", "Khung giờ bạn chọn cho ngày hôm nay đã trôi qua. Vui lòng chọn khung giờ khác muộn hơn.");
                        await PopulateViewBagsAsync(loggedInPatientId);
                        return View(model);
                    }
                }
            }
        }

        var existingSlotCount = await _context.Appointments
            .CountAsync(a => a.DoctorId == model.DoctorId
                && a.AppDate.HasValue && a.AppDate.Value.Date == model.AppDate.Value.Date
                && a.TimeSlot == model.TimeSlot
                && a.Status != "Huy");

        if (existingSlotCount >= 15)
        {
            ModelState.AddModelError("", $"Khung giờ {model.TimeSlot} của bác sĩ trong ngày này đã đầy chỗ (tối đa 15 người). Vui lòng chọn khung giờ hoặc bác sĩ khác.");
            await PopulateViewBagsAsync(loggedInPatientId);
            return View(model);
        }

        var bookingType = Request.Form["BookingType"].ToString();
        int targetPatientId = 0;

        if (bookingType == "self")
        {
            targetPatientId = loggedInPatientId;
        }
        else if (model.Patient != null)
        {
            // Xác định bệnh nhân thực tế của lịch khám (người thân)
            Patient? targetPatient = null;

            // Kiểm tra xem họ có chọn người thân đã lưu từ dropdown không
            int.TryParse(Request.Form["SelectedRelativeId"], out var selectedRelativeId);

            if (selectedRelativeId > 0)
            {
                // Xác nhận người thân này thuộc quyền quản lý của tài khoản đăng nhập để tránh khai thác bảo mật
                targetPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PatientId == selectedRelativeId && p.CreatedByPatientId == loggedInPatientId);
            }

            // Nếu chưa lưu hoặc không tìm thấy theo ID, tìm theo SĐT hoặc CCCD nhưng GIỚI HẠN chỉ tìm trong các người thân của tài khoản hiện tại
            if (targetPatient == null)
            {
                if (!string.IsNullOrWhiteSpace(model.Patient.Phone))
                {
                    targetPatient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.Phone == model.Patient.Phone && p.CreatedByPatientId == loggedInPatientId);
                }

                if (targetPatient == null && !string.IsNullOrWhiteSpace(model.Patient.CitizenId))
                {
                    targetPatient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.CitizenId == model.Patient.CitizenId && p.CreatedByPatientId == loggedInPatientId);
                }
            }

            if (targetPatient != null)
            {
                // Cập nhật thông tin bổ sung cho bệnh nhân trùng khớp (không đè giá trị rỗng)
                if (!string.IsNullOrWhiteSpace(model.Patient.FullName)) targetPatient.FullName = model.Patient.FullName;
                if (!string.IsNullOrWhiteSpace(model.Patient.Phone)) targetPatient.Phone = model.Patient.Phone;
                if (!string.IsNullOrWhiteSpace(model.Patient.Email)) targetPatient.Email = model.Patient.Email;
                if (!string.IsNullOrWhiteSpace(model.Patient.Address)) targetPatient.Address = model.Patient.Address;
                if (!string.IsNullOrWhiteSpace(model.Patient.CitizenId)) targetPatient.CitizenId = model.Patient.CitizenId;
                if (!string.IsNullOrWhiteSpace(model.Patient.Occupation)) targetPatient.Occupation = model.Patient.Occupation;
                if (!string.IsNullOrWhiteSpace(model.Patient.WorkplaceOrSchool)) targetPatient.WorkplaceOrSchool = model.Patient.WorkplaceOrSchool;
                if (!string.IsNullOrWhiteSpace(model.Patient.BhytCode)) targetPatient.BhytCode = model.Patient.BhytCode;
                if (model.Patient.Dob.HasValue) targetPatient.Dob = model.Patient.Dob;
                targetPatient.Gender = model.Patient.Gender;

                // Gắn liên kết người thân nếu chưa có và không phải là chính mình (bảo vệ tuyệt đối)
                if (targetPatient.CreatedByPatientId == null && targetPatient.PatientId != loggedInPatientId)
                {
                    targetPatient.CreatedByPatientId = loggedInPatientId;
                }

                _context.Patients.Update(targetPatient);
                await SafeSaveChangesAsync();
                
                targetPatientId = targetPatient.PatientId;
            }
            else
            {
                // Tạo một hồ sơ bệnh nhân hoàn toàn MỚI cho người thân (không trùng SĐT và CCCD dưới tài khoản hiện tại)
                var newPatient = new Patient
                {
                    PatientCode = "BN" + DateTime.Now.ToString("yyMMddHHmmss") + new Random().Next(10, 99).ToString(),
                    FullName = model.Patient.FullName ?? "Người thân",
                    Phone = model.Patient.Phone,
                    Email = model.Patient.Email,
                    Address = model.Patient.Address,
                    CitizenId = model.Patient.CitizenId,
                    Occupation = model.Patient.Occupation,
                    WorkplaceOrSchool = model.Patient.WorkplaceOrSchool,
                    BhytCode = model.Patient.BhytCode,
                    Dob = model.Patient.Dob,
                    Gender = model.Patient.Gender,
                    CreatedByPatientId = loggedInPatientId // Thiết lập liên kết người thân với tài khoản chính
                };

                _context.Patients.Add(newPatient);
                await SafeSaveChangesAsync();
                
                targetPatientId = newPatient.PatientId;
            }
        }
        else
        {
            targetPatientId = loggedInPatientId;
        }

        var newAppointment = new Appointment
        {
            PatientId = targetPatientId,
            DoctorId = model.DoctorId,
            AppDate = model.AppDate,
            TimeSlot = model.TimeSlot,
            Reason = model.Reason,
            Status = "ChoXacNhan",
            CreatedDate = DateTime.Now,
            ScheduleId = model.ScheduleId
        };

        _context.Appointments.Add(newAppointment);
        await SafeSaveChangesAsync();

        TempData["Success"] = "Đặt lịch khám thành công! Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.";
        return RedirectToAction("Index");
    }

    private async Task SafeSaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
        {
            // Tự động sửa chữa CSDL nếu vi phạm UNIQUE constraint (đặc biệt là cột CitizenID hoặc BHYT_Code trống)
            await TryFixPatientsUniqueConstraintsAsync();
            
            // Thử lưu lại lần nữa
            await _context.SaveChangesAsync();
        }
    }

    private async Task TryFixPatientsUniqueConstraintsAsync()
    {
        try
        {
            var sql = @"
                -- Đảm bảo các cài đặt để tạo Filtered Index thành công
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;

                -- 1. Sửa UNIQUE constraint cho CitizenID
                DECLARE @CitizenConstraintName NVARCHAR(200);
                SELECT TOP 1 @CitizenConstraintName = name
                FROM sys.objects
                WHERE parent_object_id = OBJECT_ID('Patients') 
                  AND type = 'UQ' 
                  AND name IN (
                      SELECT kc.name 
                      FROM sys.key_constraints kc
                      INNER JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
                      INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                      WHERE c.name = 'CitizenID'
                  );

                IF @CitizenConstraintName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE Patients DROP CONSTRAINT ' + @CitizenConstraintName);
                END

                DECLARE @CitizenIndexName NVARCHAR(200);
                SELECT TOP 1 @CitizenIndexName = i.name
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.object_id = OBJECT_ID('Patients') 
                  AND c.name = 'CitizenID' 
                  AND i.is_unique = 1 
                  AND i.is_primary_key = 0;

                IF @CitizenIndexName IS NOT NULL
                BEGIN
                    EXEC('DROP INDEX ' + @CitizenIndexName + ' ON Patients');
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Patient_CitizenID' AND object_id = OBJECT_ID('Patients'))
                BEGIN
                    EXEC('CREATE UNIQUE NONCLUSTERED INDEX UQ_Patient_CitizenID ON Patients(CitizenID) WHERE CitizenID IS NOT NULL');
                END

                -- 2. Sửa UNIQUE constraint cho BHYT_Code
                DECLARE @BhytConstraintName NVARCHAR(200);
                SELECT TOP 1 @BhytConstraintName = name
                FROM sys.objects
                WHERE parent_object_id = OBJECT_ID('Patients') 
                  AND type = 'UQ' 
                  AND name IN (
                      SELECT kc.name 
                      FROM sys.key_constraints kc
                      INNER JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
                      INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                      WHERE c.name = 'BHYT_Code'
                  );

                IF @BhytConstraintName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE Patients DROP CONSTRAINT ' + @BhytConstraintName);
                END

                DECLARE @BhytIndexName NVARCHAR(200);
                SELECT TOP 1 @BhytIndexName = i.name
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.object_id = OBJECT_ID('Patients') 
                  AND c.name = 'BHYT_Code' 
                  AND i.is_unique = 1 
                  AND i.is_primary_key = 0;

                IF @BhytIndexName IS NOT NULL
                BEGIN
                    EXEC('DROP INDEX ' + @BhytIndexName + ' ON Patients');
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Patient_BHYT_Code' AND object_id = OBJECT_ID('Patients'))
                BEGIN
                    EXEC('CREATE UNIQUE NONCLUSTERED INDEX UQ_Patient_BHYT_Code ON Patients(BHYT_Code) WHERE BHYT_Code IS NOT NULL');
                END
            ";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception)
        {
            // Bỏ qua nếu có lỗi
        }
    }
}
