using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;
using HTTTBenhVien.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class CashierWorkspaceController : Controller
    {
        private readonly HtttbenhvienContext _context;
        private readonly IVnPayService _vnPayService;

        public CashierWorkspaceController(HtttbenhvienContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // Lấy danh sách hóa đơn
            var invoices = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.InvoiceDetails)
                .OrderByDescending(i => i.InvoiceId)
                .ToListAsync();

            var unpaidInvoices = invoices.Where(i => i.Status == "ChuaThanhToan" || i.Status == "Chưa thanh toán" || i.Status == "Pending" || i.Status == "Mới" || i.Status == "New" || i.Status == "Unpaid").ToList();
            var paidTodayInvoices = invoices.Where(i => (i.Status == "DaThanhToan" || i.Status == "Đã thanh toán" || i.Status == "Paid" || i.Status == "Hoàn thành" || i.Status == "Completed") && i.PaymentDate.HasValue && i.PaymentDate.Value.Date == today).ToList();

            // Tính toán doanh thu
            ViewBag.TotalUnpaidCount = unpaidInvoices.Count;
            ViewBag.TotalUnpaidAmount = unpaidInvoices.Sum(i => i.TotalAmount ?? 0);
            
            ViewBag.TotalPaidCount = paidTodayInvoices.Count;
            ViewBag.TotalRevenueToday = paidTodayInvoices.Sum(i => i.PatientAmount ?? (i.TotalAmount ?? 0));

            ViewBag.UnpaidInvoices = unpaidInvoices;
            ViewBag.PaidInvoices = paidTodayInvoices;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int invoiceId, decimal bhytAmount, decimal discountAmount, string paymentMethod)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            if (invoice.Status == "DaThanhToan" || invoice.Status == "Đã thanh toán" || invoice.Status == "Paid" || invoice.Status == "Completed" || invoice.Status == "Hoàn thành")
            {
                TempData["Error"] = "Hóa đơn này đã được thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            // Tính toán lại số tiền
            invoice.BhytAmount = bhytAmount;
            invoice.DiscountAmount = discountAmount;
            
            var total = invoice.TotalAmount ?? 0;
            var finalAmount = total - bhytAmount - discountAmount;
            
            invoice.PatientAmount = finalAmount > 0 ? finalAmount : 0;

            // ═══════════════════════════════════════
            // Nếu chọn VNPay → Redirect sang cổng thanh toán
            // ═══════════════════════════════════════
            if (paymentMethod == "VNPay")
            {
                // Lưu tạm thông tin vào Session trước khi redirect
                HttpContext.Session.SetInt32("VnPay_InvoiceId", invoiceId);
                HttpContext.Session.SetString("VnPay_BhytAmount", bhytAmount.ToString());
                HttpContext.Session.SetString("VnPay_DiscountAmount", discountAmount.ToString());

                var paymentInfo = new PaymentInformationModel
                {
                    OrderType = "billpayment",
                    Amount = (double)(invoice.PatientAmount ?? 0),
                    OrderDescription = $"Thanh toán viện phí hóa đơn #{invoiceId}",
                    Name = invoice.Patient?.FullName ?? "Bệnh nhân"
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);
                return Redirect(paymentUrl);
            }

            // ═══════════════════════════════════════
            // Thanh toán thường (Tiền mặt, Chuyển khoản, Thẻ)
            // ═══════════════════════════════════════
            invoice.Status = "DaThanhToan";
            invoice.PaymentDate = DateTime.Now;
            invoice.PaymentMethod = string.IsNullOrEmpty(paymentMethod) ? "Tiền mặt" : paymentMethod;

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Thanh toán thành công hóa đơn #{invoice.InvoiceId} cho bệnh nhân!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════
        // VNPay Callback - Xử lý kết quả thanh toán từ VNPay trả về
        // ═══════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            var invoiceId = HttpContext.Session.GetInt32("VnPay_InvoiceId");
            var bhytStr = HttpContext.Session.GetString("VnPay_BhytAmount");
            var discountStr = HttpContext.Session.GetString("VnPay_DiscountAmount");

            if (response.Success && response.VnPayResponseCode == "00" && invoiceId.HasValue)
            {
                var invoice = await _context.Invoices.FindAsync(invoiceId.Value);
                if (invoice != null)
                {
                    decimal bhyt = 0;
                    decimal discount = 0;
                    decimal.TryParse(bhytStr, out bhyt);
                    decimal.TryParse(discountStr, out discount);

                    invoice.BhytAmount = bhyt;
                    invoice.DiscountAmount = discount;

                    var total = invoice.TotalAmount ?? 0;
                    var finalAmount = total - bhyt - discount;
                    invoice.PatientAmount = finalAmount > 0 ? finalAmount : 0;

                    invoice.Status = "DaThanhToan";
                    invoice.PaymentDate = DateTime.Now;
                    invoice.PaymentMethod = "VNPay";
                    invoice.Notes = $"VNPay TxnRef: {response.OrderId} | TransactionNo: {response.TransactionId}";

                    _context.Invoices.Update(invoice);
                    await _context.SaveChangesAsync();

                    // Xóa session tạm
                    HttpContext.Session.Remove("VnPay_InvoiceId");
                    HttpContext.Session.Remove("VnPay_BhytAmount");
                    HttpContext.Session.Remove("VnPay_DiscountAmount");

                    TempData["Success"] = $"🎉 Thanh toán VNPay thành công! Hóa đơn #{invoiceId} đã được xác nhận. Mã giao dịch: {response.TransactionId}";
                }
            }
            else
            {
                TempData["Error"] = $"❌ Thanh toán VNPay thất bại hoặc bị hủy. Mã lỗi: {response.VnPayResponseCode}. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> PrintInvoice(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Record)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }
    }
}
