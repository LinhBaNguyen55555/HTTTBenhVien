using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class InvoiceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public InvoiceController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/Invoice
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            var invoices = _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Record)
                .Include(i => i.InvoiceDetails)
                .OrderByDescending(i => i.PaymentDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                invoices = invoices.Where(i =>
                    i.Patient.FullName.Contains(searchString) ||
                    i.InvoiceId.ToString().Contains(searchString));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                invoices = invoices.Where(i => i.Status == statusFilter);
            }

            return View(await invoices.ToListAsync());
        }

        // GET: Admin/Invoice/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Record)
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();
            return View(invoice);
        }

        // GET: Admin/Invoice/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName");
            var recordList = _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.RecordId, Display = "BA #" + m.RecordId + " - " + m.Patient.FullName })
                .ToList();
            ViewData["RecordId"] = new SelectList(recordList, "RecordId", "Display");
            return View();
        }

        // POST: Admin/Invoice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int PatientId, int? RecordId, decimal? BhytAmount, decimal? DiscountAmount,
            string? PaymentMethod, string? Status, string? Notes,
            string[] ItemTypes, string[] ItemNames, int[] ItemQuantities, decimal[] ItemPrices)
        {
            var invoice = new Invoice
            {
                PatientId = PatientId,
                RecordId = RecordId,
                BhytAmount = BhytAmount ?? 0,
                DiscountAmount = DiscountAmount ?? 0,
                PaymentDate = DateTime.Now,
                PaymentMethod = string.IsNullOrEmpty(PaymentMethod) ? "Tiền mặt" : PaymentMethod,
                Status = string.IsNullOrEmpty(Status) ? "ChuaThanhToan" : Status,
                Notes = Notes
            };

            _context.Add(invoice);
            await _context.SaveChangesAsync();

            decimal total = 0;
            if (ItemNames != null)
            {
                for (int i = 0; i < ItemNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(ItemNames[i]))
                    {
                        var qty = i < ItemQuantities.Length ? ItemQuantities[i] : 1;
                        var price = i < ItemPrices.Length ? ItemPrices[i] : 0;
                        var lineTotal = qty * price;
                        total += lineTotal;

                        var detail = new InvoiceDetail
                        {
                            InvoiceId = invoice.InvoiceId,
                            ItemType = i < ItemTypes.Length ? ItemTypes[i] : "Khác",
                            ItemName = ItemNames[i],
                            Quantity = qty,
                            UnitPrice = price
                        };
                        _context.InvoiceDetails.Add(detail);
                    }
                }
            }

            invoice.TotalAmount = total;
            invoice.PatientAmount = total - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
            if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;

            _context.Update(invoice);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo hóa đơn thành công!";
            return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
        }

        // GET: Admin/Invoice/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);
            if (invoice == null) return NotFound();

            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "FullName", invoice.PatientId);
            var recordList = _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.RecordId, Display = "BA #" + m.RecordId + " - " + m.Patient.FullName })
                .ToList();
            ViewData["RecordId"] = new SelectList(recordList, "RecordId", "Display", invoice.RecordId);
            return View(invoice);
        }

        // POST: Admin/Invoice/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int PatientId, int? RecordId, decimal? BhytAmount, decimal? DiscountAmount,
            string? PaymentMethod, string? Status, string? Notes,
            string[] ItemTypes, string[] ItemNames, int[] ItemQuantities, decimal[] ItemPrices)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            invoice.PatientId = PatientId;
            invoice.RecordId = RecordId;
            invoice.BhytAmount = BhytAmount ?? 0;
            invoice.DiscountAmount = DiscountAmount ?? 0;
            invoice.PaymentMethod = PaymentMethod;
            invoice.Status = Status;
            invoice.Notes = Notes;

            // Remove old details
            _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);

            decimal total = 0;
            if (ItemNames != null)
            {
                for (int i = 0; i < ItemNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(ItemNames[i]))
                    {
                        var qty = i < ItemQuantities.Length ? ItemQuantities[i] : 1;
                        var price = i < ItemPrices.Length ? ItemPrices[i] : 0;
                        total += qty * price;

                        var detail = new InvoiceDetail
                        {
                            InvoiceId = id,
                            ItemType = i < ItemTypes.Length ? ItemTypes[i] : "Khác",
                            ItemName = ItemNames[i],
                            Quantity = qty,
                            UnitPrice = price
                        };
                        _context.InvoiceDetails.Add(detail);
                    }
                }
            }

            invoice.TotalAmount = total;
            invoice.PatientAmount = total - (invoice.BhytAmount ?? 0) - (invoice.DiscountAmount ?? 0);
            if (invoice.PatientAmount < 0) invoice.PatientAmount = 0;

            if (invoice.Status == "DaThanhToan" && invoice.PaymentDate == null)
                invoice.PaymentDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật hóa đơn thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Invoice/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();
            return View(invoice);
        }

        // POST: Admin/Invoice/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice != null)
            {
                _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa hóa đơn thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
