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
    [Authorize]
    public class HomeController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public HomeController(HtttbenhvienContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Summary Stats
            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            
            // Filter doctors by JobTitle (e.g., contains "Bác sĩ" or "Doctor")
            ViewBag.TotalDoctors = await _context.Employees
                .CountAsync(e => e.JobTitle.Contains("Bác sĩ") || e.JobTitle.Contains("Doctor"));

            // Use range comparison for better SQL translation and performance
            ViewBag.TodayAppointments = await _context.Appointments
                .CountAsync(a => a.AppDate >= today && a.AppDate < tomorrow);
            
            // Revenue (Total from Invoices)
            ViewBag.TotalRevenue = await _context.Invoices.SumAsync(i => i.TotalAmount ?? 0);
            
            // Monthly Revenue - Fix: Change InvoiceDate to PaymentDate
            ViewBag.MonthlyRevenue = await _context.Invoices
                .Where(i => i.PaymentDate.HasValue && i.PaymentDate.Value.Month == today.Month && i.PaymentDate.Value.Year == today.Year)
                .SumAsync(i => i.TotalAmount ?? 0);

            // Resources
            ViewBag.AvailableBeds = await _context.Beds.CountAsync(b => b.IsOccupied == false || b.IsOccupied == null);
            ViewBag.LowStockMedicine = await _context.Medicines.CountAsync(m => m.StockQuantity <= 50);
            ViewBag.TotalSupplies = await _context.MedicalSupplies.CountAsync();
            
            // Lists for Tables
            ViewBag.RecentAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .OrderByDescending(a => a.AppDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentPatients = await _context.Patients
                .OrderByDescending(p => p.PatientId)
                .Take(5)
                .ToListAsync();

            // Recent Invoices - Fix: Change InvoiceDate to PaymentDate
            ViewBag.RecentInvoices = await _context.Invoices
                .Include(i => i.Patient)
                .OrderByDescending(i => i.PaymentDate)
                .Take(5)
                .ToListAsync();

            // Stats for chart (simplified - daily appointments for last 7 days)
            var startDate = today.AddDays(-6);
            var last7Days = Enumerable.Range(0, 7).Select(i => startDate.AddDays(i)).ToList();
            
            var appointmentStats = await _context.Appointments
                .Where(a => a.AppDate >= startDate && a.AppDate < tomorrow)
                .GroupBy(a => a.AppDate.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.ChartLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();
            ViewBag.ChartData = last7Days.Select(d => appointmentStats.FirstOrDefault(s => s.Date == d.Date)?.Count ?? 0).ToList();

            // Pie chart - Appointment status breakdown with translation
            var statusGroups = await _context.Appointments
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key ?? "Không rõ", Count = g.Count() })
                .ToListAsync();
            
            var statusMapping = new Dictionary<string, string> {
                { "New", "Mới" },
                { "Confirmed", "Xác nhận" },
                { "Completed", "Hoàn thành" },
                { "Cancelled", "Đã hủy" },
                { "Pending", "Đang chờ" }
            };

            ViewBag.PieLabels = statusGroups.Select(s => statusMapping.ContainsKey(s.Status) ? statusMapping[s.Status] : s.Status).ToList();
            ViewBag.PieData = statusGroups.Select(s => s.Count).ToList();

            // Doughnut chart - Revenue by payment method with translation
            var paymentGroups = await _context.Invoices
                .GroupBy(i => i.PaymentMethod)
                .Select(g => new { Method = g.Key ?? "Khác", Total = g.Sum(x => x.TotalAmount ?? 0) })
                .ToListAsync();

            var paymentMapping = new Dictionary<string, string> {
                { "Cash", "Tiền mặt" },
                { "Transfer", "Chuyển khoản" },
                { "CreditCard", "Thẻ tín dụng" },
                { "BHYT", "Bảo hiểm y tế" }
            };

            ViewBag.DoughnutLabels = paymentGroups.Select(p => paymentMapping.ContainsKey(p.Method) ? paymentMapping[p.Method] : p.Method).ToList();
            ViewBag.DoughnutData = paymentGroups.Select(p => p.Total).ToList();

            // Radar Chart - Top 5 Departments by Activity (Appointments)
            var deptStats = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d.Dept)
                .Where(a => a.Doctor != null && a.Doctor.Dept != null)
                .GroupBy(a => a.Doctor.Dept.DeptName)
                .Select(g => new { DeptName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            ViewBag.RadarLabels = deptStats.Select(d => d.DeptName).ToList();
            ViewBag.RadarData = deptStats.Select(d => d.Count).ToList();

            // Handle empty case
            if (ViewBag.RadarLabels.Count == 0) {
                ViewBag.RadarLabels = new List<string> { "Khoa Nội", "Khoa Ngoại", "Sản", "Nhi", "Cấp cứu" };
                ViewBag.RadarData = new List<int> { 0, 0, 0, 0, 0 };
            }

            return View();
        }
    }
}
