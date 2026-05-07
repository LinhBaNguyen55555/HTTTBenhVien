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
    public class FinancialReportController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public FinancialReportController(HtttbenhvienContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? month, int? year, string timeRange = "this_month")
        {
            var now = DateTime.Now;
            var selectedMonth = month ?? now.Month;
            var selectedYear = year ?? now.Year;

            var query = _context.Invoices.Where(i => (i.Status == "DaThanhToan" || i.Status == "Đã thanh toán" || i.Status == "Paid" || i.Status == "Hoàn thành" || i.Status == "Completed") && i.PaymentDate != null).AsQueryable();

            DateTime? startDate = null;
            DateTime? endDate = null;
            string reportTitle = "";
            string chartTitle = "";

            if (timeRange == "all_time")
            {
                reportTitle = "Toàn thời gian";
                chartTitle = "Biểu đồ dòng tiền (Theo năm)";
            }
            else if (timeRange == "this_year")
            {
                startDate = new DateTime(now.Year, 1, 1);
                endDate = new DateTime(now.Year, 12, 31, 23, 59, 59);
                reportTitle = $"Năm {now.Year}";
                chartTitle = "Biểu đồ dòng tiền (Theo tháng)";
                query = query.Where(i => i.PaymentDate >= startDate && i.PaymentDate <= endDate);
            }
            else if (timeRange == "custom_month")
            {
                startDate = new DateTime(selectedYear, selectedMonth, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                reportTitle = $"Tháng {selectedMonth}/{selectedYear}";
                chartTitle = "Biểu đồ dòng tiền (Theo ngày)";
                query = query.Where(i => i.PaymentDate >= startDate && i.PaymentDate <= endDate);
            }
            else // "this_month"
            {
                startDate = new DateTime(now.Year, now.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                reportTitle = $"Tháng {now.Month}/{now.Year}";
                chartTitle = "Biểu đồ dòng tiền (Theo ngày)";
                query = query.Where(i => i.PaymentDate >= startDate && i.PaymentDate <= endDate);
            }

            var paidInvoices = await query.ToListAsync();

            var totalRevenue = paidInvoices.Sum(i => i.PatientAmount ?? (i.TotalAmount ?? 0));
            var totalDiscount = paidInvoices.Sum(i => (i.BhytAmount ?? 0) + (i.DiscountAmount ?? 0));
            var totalTransactions = paidInvoices.Count;

            // Nợ (Luôn lấy toàn thời gian)
            var totalUnpaid = await _context.Invoices
                .Where(i => i.Status == "ChuaThanhToan" || i.Status == "Chưa thanh toán" || i.Status == "Pending" || i.Status == "Mới" || i.Status == "New" || i.Status == "Unpaid")
                .SumAsync(i => i.TotalAmount ?? 0);

            // Chart Data
            var labels = new List<string>();
            var revenueData = new List<decimal>();

            if (timeRange == "this_month" || timeRange == "custom_month")
            {
                // By Day
                int daysInMonth = DateTime.DaysInMonth(startDate.Value.Year, startDate.Value.Month);
                for (int i = 1; i <= daysInMonth; i++)
                {
                    labels.Add($"{i}/{startDate.Value.Month}");
                    var dailyRevenue = paidInvoices
                        .Where(x => x.PaymentDate.Value.Day == i)
                        .Sum(x => x.PatientAmount ?? (x.TotalAmount ?? 0));
                    revenueData.Add(dailyRevenue);
                }
            }
            else if (timeRange == "this_year")
            {
                // By Month
                for (int i = 1; i <= 12; i++)
                {
                    labels.Add($"Tháng {i}");
                    var monthlyRevenue = paidInvoices
                        .Where(x => x.PaymentDate.Value.Month == i)
                        .Sum(x => x.PatientAmount ?? (x.TotalAmount ?? 0));
                    revenueData.Add(monthlyRevenue);
                }
            }
            else // all_time
            {
                // Group by Year
                if (paidInvoices.Any())
                {
                    var minYear = paidInvoices.Min(x => x.PaymentDate.Value.Year);
                    var maxYear = Math.Max(minYear, now.Year);
                    for (int i = minYear; i <= maxYear; i++)
                    {
                        labels.Add($"Năm {i}");
                        var yearlyRevenue = paidInvoices
                            .Where(x => x.PaymentDate.Value.Year == i)
                            .Sum(x => x.PatientAmount ?? (x.TotalAmount ?? 0));
                        revenueData.Add(yearlyRevenue);
                    }
                }
            }

            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = revenueData;

            // Pie chart
            var paymentMethods = paidInvoices
                .GroupBy(i => i.PaymentMethod ?? "Tiền mặt")
                .Select(g => new { Method = g.Key, Amount = g.Sum(i => i.PatientAmount ?? (i.TotalAmount ?? 0)) })
                .ToList();

            ViewBag.PieLabels = paymentMethods.Select(p => p.Method).ToList();
            ViewBag.PieData = paymentMethods.Select(p => p.Amount).ToList();

            // Top transactions
            var topTransactions = paidInvoices
                .OrderByDescending(i => i.PatientAmount)
                .Take(5)
                .ToList();

            foreach (var item in topTransactions)
            {
                _context.Entry(item).Reference(i => i.Patient).Load();
            }

            ViewBag.TopTransactions = topTransactions;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalDiscount = totalDiscount;
            ViewBag.TotalUnpaid = totalUnpaid;
            ViewBag.TotalTransactions = totalTransactions;
            
            ViewBag.ReportTitle = reportTitle;
            ViewBag.ChartTitle = chartTitle;
            ViewBag.CurrentTimeRange = timeRange;
            ViewBag.CurrentMonth = selectedMonth;
            ViewBag.CurrentYear = selectedYear;

            return View();
        }
    }
}
