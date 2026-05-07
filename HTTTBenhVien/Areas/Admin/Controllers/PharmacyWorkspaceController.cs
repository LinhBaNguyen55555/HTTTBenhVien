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
            ViewBag.PendingPrescriptions = pendingPrescriptions;
            ViewBag.PendingCount = pendingPrescriptions.Count;

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
        // CẤP PHÁT THUỐC: Xác nhận đã phát thuốc cho bệnh nhân
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

            // Kiểm tra tồn kho trước khi cấp
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

            // Trừ tồn kho
            foreach (var detail in prescription.PrescriptionDetails)
            {
                var medicine = await _context.Medicines.FindAsync(detail.MedicineId);
                if (medicine != null)
                {
                    medicine.StockQuantity -= detail.Quantity ?? 0;
                    _context.Medicines.Update(medicine);
                }
            }

            // Cập nhật trạng thái đơn thuốc
            prescription.Status = "DaCapPhat";

            _context.Prescriptions.Update(prescription);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Cấp phát thuốc thành công cho đơn #{prescriptionId}! Tồn kho đã được cập nhật.";
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
