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
    public class PharmacyWorkspaceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public PharmacyWorkspaceController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════
        // TRANG CHỦ: Dashboard Dược & Vật tư
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> Index()
        {
            // ─── Thống kê Thuốc ───
            var medicines = await _context.Medicines.ToListAsync();
            ViewBag.TotalMedicines = medicines.Count;
            ViewBag.LowStockMedicines = medicines.Where(m => m.StockQuantity <= 50).ToList();
            ViewBag.LowStockCount = ViewBag.LowStockMedicines.Count;
            ViewBag.ExpiringMedicines = medicines.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= DateTime.Now.AddDays(90)).ToList();
            ViewBag.ExpiringCount = ViewBag.ExpiringMedicines.Count;
            ViewBag.ExpiredMedicines = medicines.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value < DateTime.Now).ToList();
            ViewBag.ExpiredCount = ViewBag.ExpiredMedicines.Count;
            ViewBag.TotalStockValue = medicines.Sum(m => m.Price * m.StockQuantity);

            // ─── Thống kê Vật tư ───
            var supplies = await _context.MedicalSupplies.ToListAsync();
            ViewBag.TotalSupplies = supplies.Count;
            ViewBag.LowStockSupplies = supplies.Where(s => s.StockQuantity <= 20).ToList();
            ViewBag.LowStockSupplyCount = ViewBag.LowStockSupplies.Count;
            ViewBag.ExpiringSupplies = supplies.Where(s => s.ExpiryDate.HasValue && s.ExpiryDate.Value <= DateTime.Now.AddDays(90)).ToList();
            ViewBag.ExpiringSupplyCount = ViewBag.ExpiringSupplies.Count;

            // ─── Đơn thuốc chờ cấp phát ───
            var pendingPrescriptions = await _context.Prescriptions
                .Include(p => p.Record).ThenInclude(r => r.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
                .Where(p => p.Status == "ChoCapPhat" || p.Status == "Chờ cấp phát" || p.Status == "Pending" || p.Status == "Mới" || p.Status == "New")
                .OrderByDescending(p => p.CreateDate)
                .ToListAsync();
            
            // Xác thực trạng thái thanh toán & Số tiền cần thu tại quầy Dược
            var recordIds = pendingPrescriptions.Select(p => p.RecordId).Distinct().ToList();
            var invoices = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Where(i => i.RecordId != null && recordIds.Contains(i.RecordId.Value))
                .ToListAsync();

            var paidPrescriptionIds = new List<int>();
            var unpaidAmounts = new Dictionary<int, decimal>();

            foreach (var p in pendingPrescriptions)
            {
                var medIds = p.PrescriptionDetails.Select(pd => pd.MedicineId).ToList();
                
                // Trạng thái thanh toán của Thuốc trong hồ sơ này
                bool isPaid = invoices.Any(i => i.RecordId == p.RecordId 
                                            && i.Status == "DaThanhToan" 
                                            && i.InvoiceDetails.Any(d => d.ItemType == "Thuốc" && medIds.Contains(d.ItemId ?? 0)));
                if (isPaid)
                {
                    paidPrescriptionIds.Add(p.PrescriptionId);
                }
                else
                {
                    // Lấy số tiền chưa thanh toán tương ứng với Thuốc trong record
                    var unpaidInvoice = invoices.FirstOrDefault(i => i.RecordId == p.RecordId 
                                                && i.Status != "DaThanhToan" 
                                                && i.Status != "Huy"
                                                && i.InvoiceDetails.Any(d => d.ItemType == "Thuốc" && medIds.Contains(d.ItemId ?? 0)));
                    if (unpaidInvoice != null)
                    {
                        unpaidAmounts[p.PrescriptionId] = unpaidInvoice.PatientAmount ?? 0;
                    }
                    else
                    {
                        decimal total = 0;
                        foreach (var detail in p.PrescriptionDetails)
                        {
                            total += (detail.Quantity ?? 0) * (detail.Medicine?.Price ?? 0);
                        }
                        unpaidAmounts[p.PrescriptionId] = total;
                    }
                }
            }

            ViewBag.PendingPrescriptions = pendingPrescriptions;
            ViewBag.PendingCount = pendingPrescriptions.Count;
            ViewBag.PaidPrescriptionIds = paidPrescriptionIds;
            ViewBag.UnpaidAmounts = unpaidAmounts;

            // ─── Đơn thuốc đã cấp hôm nay ───
            var today = DateTime.Today;
            var dispensedToday = await _context.Prescriptions
                .Include(p => p.Record).ThenInclude(r => r.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
                .Where(p => (p.Status == "DaCapPhat" || p.Status == "Đã cấp phát" || p.Status == "Dispensed" || p.Status == "Completed" || p.Status == "Hoàn thành"))
                .OrderByDescending(p => p.CreateDate)
                .Take(20)
                .ToListAsync();
            ViewBag.DispensedToday = dispensedToday;

            return View();
        }

        // ═══════════════════════════════════════════════════════
        // CẤP PHÁT THUỐC (Nhanh): Xác nhận đã phát thuốc (chỉ dành cho đơn ĐÃ đóng tiền)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DispensePrescription(int prescriptionId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null)
            {
                TempData["Error"] = "Không tìm thấy đơn thuốc.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Kiểm tra trạng thái thanh toán trước khi cấp phát nhanh
            var medIds = prescription.PrescriptionDetails.Select(pd => pd.MedicineId).ToList();
            bool isPaid = await _context.InvoiceDetails
                .Include(id => id.Invoice)
                .AnyAsync(id => id.Invoice.RecordId == prescription.RecordId 
                             && id.ItemType == "Thuốc" 
                             && medIds.Contains(id.ItemId ?? 0)
                             && id.Invoice.Status == "DaThanhToan");

            if (!isPaid)
            {
                TempData["Error"] = "Đơn thuốc này chưa được thanh toán! Vui lòng chọn 'Thu tiền & Cấp phát' để thanh toán trực tiếp tại nhà thuốc.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Kiểm tra tồn kho trước khi cấp
            var insufficientItems = new List<string>();
            foreach (var detail in prescription.PrescriptionDetails)
            {
                var medicine = await _context.Medicines.FindAsync(detail.MedicineId);
                if (medicine != null)
                {
                    var qty = detail.Quantity ?? 0;
                    if (medicine.StockQuantity < qty)
                    {
                        insufficientItems.Add($"{medicine.MedicineName} (Cần: {qty}, Kho: {medicine.StockQuantity})");
                    }
                }
            }

            if (insufficientItems.Any())
            {
                TempData["Error"] = "Không đủ tồn kho: " + string.Join(", ", insufficientItems);
                return RedirectToAction(nameof(Index));
            }

            // 3. Trừ tồn kho
            foreach (var detail in prescription.PrescriptionDetails)
            {
                var medicine = await _context.Medicines.FindAsync(detail.MedicineId);
                if (medicine != null)
                {
                    medicine.StockQuantity -= detail.Quantity ?? 0;
                    _context.Medicines.Update(medicine);
                }
            }

            // 4. Cập nhật trạng thái đơn thuốc
            prescription.Status = "DaCapPhat";

            _context.Prescriptions.Update(prescription);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cấp phát nhanh thành công đơn thuốc #{prescriptionId}!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════
        // THU TIỀN VÀ CẤP PHÁT (Thanh toán tại quầy Dược)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAndDispensePrescription(int prescriptionId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Record).ThenInclude(r => r.Patient)
                .Include(p => p.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null)
            {
                TempData["Error"] = "Không tìm thấy đơn thuốc.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Kiểm tra tồn kho trước khi thanh toán & cấp phát
            var insufficientItems = new List<string>();
            foreach (var detail in prescription.PrescriptionDetails)
            {
                var medicine = await _context.Medicines.FindAsync(detail.MedicineId);
                if (medicine != null)
                {
                    var qty = detail.Quantity ?? 0;
                    if (medicine.StockQuantity < qty)
                    {
                        insufficientItems.Add($"{medicine.MedicineName} (Cần: {qty}, Kho: {medicine.StockQuantity})");
                    }
                }
            }

            if (insufficientItems.Any())
            {
                TempData["Error"] = "Không đủ tồn kho: " + string.Join(", ", insufficientItems);
                return RedirectToAction(nameof(Index));
            }

            // 2. Tìm hóa đơn chưa thanh toán chứa Thuốc của đơn này
            var medIds = prescription.PrescriptionDetails.Select(pd => pd.MedicineId).ToList();
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.RecordId == prescription.RecordId 
                                          && i.Status != "DaThanhToan" 
                                          && i.Status != "Huy"
                                          && i.InvoiceDetails.Any(d => d.ItemType == "Thuốc" && medIds.Contains(d.ItemId ?? 0)));

            if (invoice != null)
            {
                invoice.Status = "DaThanhToan";
                invoice.PaymentDate = DateTime.Now;
                invoice.PaymentMethod = "Tiền mặt tại nhà thuốc";
                _context.Invoices.Update(invoice);
            }
            else
            {
                // Nếu chưa có hóa đơn, tự tạo hóa đơn đã thanh toán để đảm bảo doanh thu thống nhất
                decimal total = 0;
                foreach (var detail in prescription.PrescriptionDetails)
                {
                    total += (detail.Quantity ?? 0) * (detail.Medicine?.Price ?? 0);
                }
                
                var newInvoice = new Invoice
                {
                    PatientId = prescription.Record.PatientId,
                    RecordId = prescription.RecordId,
                    TotalAmount = total,
                    BhytAmount = 0,
                    DiscountAmount = 0,
                    PatientAmount = total,
                    Status = "DaThanhToan",
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "Tiền mặt tại nhà thuốc",
                    Notes = $"Thanh toán trực tiếp tại nhà thuốc cho đơn #{prescriptionId}"
                };
                _context.Invoices.Add(newInvoice);
                await _context.SaveChangesAsync();

                foreach (var detail in prescription.PrescriptionDetails)
                {
                    var invDetail = new InvoiceDetail
                    {
                        InvoiceId = newInvoice.InvoiceId,
                        ItemType = "Thuốc",
                        ItemId = detail.MedicineId,
                        ItemName = detail.Medicine?.MedicineName,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.Medicine?.Price
                    };
                    _context.InvoiceDetails.Add(invDetail);
                }
            }

            // 3. Trừ tồn kho thuốc
            foreach (var detail in prescription.PrescriptionDetails)
            {
                var medicine = await _context.Medicines.FindAsync(detail.MedicineId);
                if (medicine != null)
                {
                    medicine.StockQuantity -= detail.Quantity ?? 0;
                    _context.Medicines.Update(medicine);
                }
            }

            // 4. Cập nhật trạng thái đơn thuốc
            prescription.Status = "DaCapPhat";
            _context.Prescriptions.Update(prescription);
            
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thu tiền mặt tại nhà thuốc và cấp phát thành công đơn thuốc #{prescriptionId}!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════
        // IN PHIẾU XUẤT THUỐC
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> PrintPrescription(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Record).ThenInclude(r => r.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionDetails).ThenInclude(pd => pd.Medicine)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null) return NotFound();

            return View(prescription);
        }
    }
}
