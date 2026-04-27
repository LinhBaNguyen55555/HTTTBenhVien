using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly HtttbenhvienContext _context;

    public DashboardController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        var totalPatients = await _context.Patients.CountAsync();
        var todayAppointments = await _context.Appointments.CountAsync(a => a.AppDate.HasValue && a.AppDate.Value.Date == today);
        var pendingAppointments = await _context.Appointments.CountAsync(a => a.Status == "New" || a.Status == "DaXacNhan");
        var todayRecords = await _context.MedicalRecords.CountAsync(m => m.VisitDate.HasValue && m.VisitDate.Value.Date == today);
        var totalEmployees = await _context.Employees.CountAsync();
        var lowStockMedicines = await _context.Medicines.CountAsync(m => m.StockQuantity < 500);
        var unreadContacts = await _context.Contacts.CountAsync(c => c.Status == "New");
        var totalArticles = await _context.Articles.CountAsync();

        decimal todayRevenue = 0;
        var todayInvoices = await _context.Invoices
            .Where(i => i.PaymentDate.HasValue && i.PaymentDate.Value.Date == today && i.Status == "DaThanhToan")
            .Include(i => i.InvoiceDetails)
            .ToListAsync();
        foreach (var inv in todayInvoices)
        {
            var itemsTotal = inv.InvoiceDetails.Sum(d => (d.UnitPrice ?? 0) * (d.Quantity ?? 0));
            todayRevenue += itemsTotal - (inv.DiscountAmount ?? 0);
        }

        var stats = new
        {
            TotalPatients = totalPatients,
            TodayAppointments = todayAppointments,
            PendingAppointments = pendingAppointments,
            TodayMedicalRecords = todayRecords,
            TotalEmployees = totalEmployees,
            LowStockMedicines = lowStockMedicines,
            TodayRevenue = todayRevenue,
            TotalArticles = totalArticles,
            UnreadContacts = unreadContacts,
        };

        var recentPatients = await _context.Patients
            .OrderByDescending(p => p.PatientId)
            .Take(5)
            .ToListAsync();

        var recentAppointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.AppDate.HasValue && a.AppDate.Value.Date >= today)
            .OrderBy(a => a.AppDate)
            .Take(8)
            .ToListAsync();

        ViewBag.Stats = stats;
        ViewBag.RecentPatients = recentPatients;
        ViewBag.RecentAppointments = recentAppointments;

        return View();
    }
}
