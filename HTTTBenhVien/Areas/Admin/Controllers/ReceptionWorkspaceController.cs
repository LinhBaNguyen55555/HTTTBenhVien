using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ReceptionWorkspaceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public ReceptionWorkspaceController(HtttbenhvienContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Thống kê hôm nay
            var appointmentsToday = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppDate >= today && a.AppDate < tomorrow)
                .OrderBy(a => a.TimeSlot)
                .ToListAsync();

            ViewBag.TotalAppointments = appointmentsToday.Count;
            ViewBag.TotalPending = appointmentsToday.Count(a => a.Status == "Pending" || a.Status == "Chờ xác nhận" || a.Status == "New" || a.Status == "Mới" || a.Status == "Đang chờ");
            ViewBag.TotalConfirmed = appointmentsToday.Count(a => a.Status == "Confirmed" || a.Status == "Đã xác nhận" || a.Status == "Xác nhận");
            ViewBag.TotalCheckedIn = appointmentsToday.Count(a => a.Status == "CheckedIn" || a.Status == "Đã tiếp nhận" || a.Status == "Completed" || a.Status == "Hoàn thành");
            ViewBag.TotalCancelled = appointmentsToday.Count(a => a.Status == "Cancelled" || a.Status == "Đã hủy");

            // Lấy danh sách bác sĩ CÓ LỊCH LÀM VIỆC trong ngày hôm nay và đang mở nhận bệnh nhân
            ViewBag.Doctors = await _context.DoctorSchedules
                .Include(ds => ds.Doctor)
                .Where(ds => ds.WorkDate.Date == today && ds.IsAvailable == true)
                .Select(ds => new { EmpId = ds.DoctorId, FullName = ds.Doctor.FullName + " - Ca " + ds.ShiftType })
                .Distinct()
                .ToListAsync();

            return View(appointmentsToday);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật trạng thái lịch hẹn thành: {status}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int appointmentId, int doctorId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null)
            {
                TempData["Error"] = "Không tìm thấy lịch hẹn.";
                return RedirectToAction(nameof(Index));
            }

            // Tạo bệnh án mới
            var record = new MedicalRecord
            {
                PatientId = appointment.PatientId,
                DoctorId = doctorId,
                AppointmentId = appointmentId,
                VisitDate = DateTime.Now,
                RecordType = "Khám bệnh",
                Notes = appointment.Reason
            };

            _context.MedicalRecords.Add(record);
            
            // Đổi trạng thái lịch hẹn
            appointment.Status = "CheckedIn";
            _context.Appointments.Update(appointment);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã tiếp nhận bệnh nhân và chuyển sang Phòng khám thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WalkIn(string FullName, string PhoneNumber, string Gender, DateTime DateOfBirth, int DoctorId, string Reason)
        {
            // Kiểm tra xem bệnh nhân đã tồn tại qua SĐT chưa
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Phone == PhoneNumber);
            
            if (patient == null)
            {
                // Tạo bệnh nhân mới
                patient = new Patient
                {
                    FullName = FullName,
                    Phone = PhoneNumber,
                    Gender = Gender == "Nam",
                    Dob = DateOfBirth,
                    Address = "Khách vãng lai"
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }

            // Trực tiếp tạo Bệnh án (Không cần qua Appointment)
            var record = new MedicalRecord
            {
                PatientId = patient.PatientId,
                DoctorId = DoctorId,
                VisitDate = DateTime.Now,
                RecordType = "Khám trực tiếp",
                Notes = Reason
            };

            _context.MedicalRecords.Add(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã đăng ký khám trực tiếp cho bệnh nhân {patient.FullName} thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
