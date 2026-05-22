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
    public class LabWorkspaceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public LabWorkspaceController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════
        // TRANG CHỦ: Dashboard Cận lâm sàng
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // Tất cả phiếu dịch vụ cận lâm sàng
            var allRequests = await _context.RecordServices
                .Include(rs => rs.Record).ThenInclude(r => r.Patient)
                .Include(rs => rs.Service)
                .Include(rs => rs.Doctor)
                .OrderByDescending(rs => rs.RsId)
                .ToListAsync();

            // ─── Phân loại trạng thái ───
            var pending = allRequests.Where(r =>
                r.Status == "ChoThucHien" || r.Status == "Chờ thực hiện" ||
                r.Status == "Pending" || r.Status == "New" || r.Status == "Mới" ||
                string.IsNullOrEmpty(r.Status)).ToList();

            var inProgress = allRequests.Where(r =>
                r.Status == "DangThucHien" || r.Status == "Đang thực hiện" ||
                r.Status == "InProgress" || r.Status == "Processing").ToList();

            var completedToday = allRequests.Where(r =>
                (r.Status == "HoanThanh" || r.Status == "Hoàn thành" ||
                 r.Status == "Completed" || r.Status == "Done" || r.Status == "DaCoKetQua") &&
                r.ExecuteDate.HasValue && r.ExecuteDate.Value.Date == today).ToList();

            var completedAll = allRequests.Where(r =>
                r.Status == "HoanThanh" || r.Status == "Hoàn thành" ||
                r.Status == "Completed" || r.Status == "Done" || r.Status == "DaCoKetQua").ToList();

            // ─── Truy vấn Trạng thái Thanh toán cận lâm sàng ───
            var recordIds = allRequests.Select(r => r.RecordId).Distinct().ToList();
            var paidInvoiceDetails = await _context.InvoiceDetails
                .Include(id => id.Invoice)
                .Where(id => id.Invoice.RecordId != null && recordIds.Contains(id.Invoice.RecordId.Value) 
                             && id.ItemType == "Dịch vụ" 
                             && id.Invoice.Status == "DaThanhToan")
                .ToListAsync();

            var paidRsIds = new List<int>();
            foreach (var rs in allRequests)
            {
                if (rs.Record?.IsEmergency == true || paidInvoiceDetails.Any(id => id.Invoice.RecordId == rs.RecordId && id.ItemId == rs.ServiceId))
                {
                    paidRsIds.Add(rs.RsId);
                }
            }
            ViewBag.PaidRsIds = paidRsIds;

            var emergencyRsIds = allRequests.Where(r => r.Record?.IsEmergency == true).Select(r => r.RsId).ToList();
            ViewBag.EmergencyRsIds = emergencyRsIds;

            // ─── Thống kê ───
            ViewBag.PendingCount = pending.Count;
            ViewBag.InProgressCount = inProgress.Count;
            ViewBag.CompletedTodayCount = completedToday.Count;
            ViewBag.TotalCompletedCount = completedAll.Count;
            ViewBag.TotalRequestsToday = allRequests.Count(r => r.ExecuteDate.HasValue && r.ExecuteDate.Value.Date == today);

            // ─── Thống kê theo loại dịch vụ (Top 5) ───
            var serviceStats = allRequests
                .GroupBy(r => r.Service?.ServiceName ?? "Không xác định")
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToList();
            ViewBag.ServiceStatNames = serviceStats.Select(s => s.Name).ToList();
            ViewBag.ServiceStatCounts = serviceStats.Select(s => s.Count).ToList();

            ViewBag.PendingRequests = pending;
            ViewBag.InProgressRequests = inProgress;
            ViewBag.CompletedRequests = completedAll.Take(30).ToList();

            return View();
        }

        // ═══════════════════════════════════════════════════════
        // NHẬN PHIẾU: Chuyển trạng thái từ "Chờ" → "Đang thực hiện"
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int rsId)
        {
            var rs = await _context.RecordServices
                .Include(r => r.Record)
                .FirstOrDefaultAsync(r => r.RsId == rsId);
            if (rs == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            // KIỂM TRA THANH TOÁN (Bypass nếu là ca Cấp cứu)
            bool isEmergency = rs.Record?.IsEmergency == true;
            var isPaid = isEmergency || await _context.InvoiceDetails
                .Include(id => id.Invoice)
                .AnyAsync(id => id.Invoice.RecordId == rs.RecordId 
                              && id.ItemType == "Dịch vụ" 
                              && id.ItemId == rs.ServiceId 
                              && id.Invoice.Status == "DaThanhToan");

            if (!isPaid)
            {
                TempData["Error"] = "Dịch vụ cận lâm sàng này chưa được thanh toán! Bệnh nhân cần đóng phí trước khi thực hiện.";
                return RedirectToAction(nameof(Index));
            }

            rs.Status = "DangThucHien";
            rs.ExecuteDate = DateTime.Now;
            _context.RecordServices.Update(rs);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã nhận phiếu #{rsId}. Bắt đầu thực hiện xét nghiệm/siêu âm!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════
        // TRẢ KẾT QUẢ: Nhập kết quả + Chuyển trạng thái → "Hoàn thành"
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitResult(int rsId, string resultValue, string resultImages)
        {
            var rs = await _context.RecordServices.FindAsync(rsId);
            if (rs == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            rs.ResultValue = resultValue;
            rs.ResultImages = resultImages;
            rs.Status = "HoanThanh";
            rs.ExecuteDate = DateTime.Now;

            _context.RecordServices.Update(rs);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã trả kết quả cho phiếu #{rsId} thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════
        // IN PHIẾU KẾT QUẢ
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> PrintResult(int id)
        {
            var rs = await _context.RecordServices
                .Include(r => r.Record).ThenInclude(r => r.Patient)
                .Include(r => r.Service)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.RsId == id);

            if (rs == null) return NotFound();

            return View(rs);
        }
    }
}
