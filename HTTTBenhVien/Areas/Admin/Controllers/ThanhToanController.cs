using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ThanhToanController : Controller
{
    private readonly HtttbenhvienContext _context;

    public ThanhToanController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        int pageSize = 25;
        var query = _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.InvoiceDetails)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(i => i.Patient != null && i.Patient.FullName != null
                && i.Patient.FullName.ToLower().Contains(search));
        }

        ViewBag.Search = search;
        var total = await query.CountAsync();
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var invoices = await query
            .OrderByDescending(i => i.InvoiceId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var todayRevenue = await _context.Invoices
            .Where(i => i.PaymentDate.HasValue && i.PaymentDate.Value.Date == DateTime.Today && i.Status == "DaThanhToan")
            .Include(i => i.InvoiceDetails)
            .ToListAsync();

        decimal totalRev = 0;
        foreach (var inv in todayRevenue)
        {
            var itemsTotal = inv.InvoiceDetails.Sum(d => (d.UnitPrice ?? 0) * (d.Quantity ?? 0));
            totalRev += itemsTotal - (inv.DiscountAmount ?? 0);
        }

        ViewBag.TodayRevenue = totalRev;
        return View(invoices);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int recordId)
    {
        var record = await _context.MedicalRecords
            .Include(m => m.Patient)
            .Include(m => m.RecordServices)
            .ThenInclude(rs => rs.Service)
            .FirstOrDefaultAsync(m => m.RecordId == recordId);

        if (record == null) return NotFound();

        var existingInvoice = await _context.Invoices.FirstOrDefaultAsync(i => i.RecordId == recordId);
        if (existingInvoice != null)
            return RedirectToAction("Edit", new { id = existingInvoice.InvoiceId });

        var model = new ThanhToanViewModel
        {
            RecordId = recordId,
            PatientId = record.PatientId,
            PatientName = record.Patient?.FullName,
            PatientCode = record.Patient?.PatientCode,
            BhytCode = record.Patient?.BhytCode,
            VisitDate = record.VisitDate,
            Items = new List<InvoiceItemViewModel>()
        };

        if (record.RecordServices != null)
        {
            foreach (var svc in record.RecordServices)
            {
                model.Items.Add(new InvoiceItemViewModel
                {
                    ItemName = svc.Service?.ServiceName ?? "",
                    ItemType = "DichVu",
                    UnitPrice = svc.Service?.BasePrice ?? 0,
                    Quantity = 1
                });
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThanhToanViewModel model)
    {
        if (!ModelState.IsValid || model.Items == null || !model.Items.Any())
        {
            if (model.RecordId > 0)
                return RedirectToAction("Create", new { recordId = model.RecordId });
            return RedirectToAction("Index");
        }

        var total = model.Items.Sum(i => i.UnitPrice * i.Quantity);
        decimal bhytAmount = 0;
        decimal patientAmount = total;

        if (!string.IsNullOrEmpty(model.BhytCode))
        {
            bhytAmount = (int)(total * 0.8m);
            patientAmount = total - bhytAmount;
        }

        var invoice = new Invoice
        {
            PatientId = model.PatientId,
            RecordId = model.RecordId,
            TotalAmount = total,
            BhytAmount = bhytAmount,
            DiscountAmount = model.DiscountAmount,
            PatientAmount = patientAmount - (model.DiscountAmount == 0 ? 0m : model.DiscountAmount),
            PaymentDate = DateTime.Now,
            PaymentMethod = model.PaymentMethod,
            Status = "DaThanhToan",
            Notes = model.Notes
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        foreach (var item in model.Items)
        {
            var detail = new InvoiceDetail
            {
                InvoiceId = invoice.InvoiceId,
                ItemName = item.ItemName,
                ItemType = item.ItemType,
                ItemId = item.ItemId,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            };
            _context.InvoiceDetails.Add(detail);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thanh toan thanh cong. Tong tien: " + invoice.PatientAmount.Value.ToString("N0") + " VND";
        return RedirectToAction("Edit", new { id = invoice.InvoiceId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.InvoiceDetails)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice == null) return NotFound();

        var model = new ThanhToanViewModel
        {
            InvoiceId = invoice.InvoiceId,
            RecordId = invoice.RecordId ?? 0,
            PatientId = invoice.PatientId,
            PatientName = invoice.Patient?.FullName,
            PatientCode = invoice.Patient?.PatientCode,
            BhytCode = invoice.Patient?.BhytCode,
            TotalAmount = invoice.TotalAmount ?? 0,
            BhytAmount = invoice.BhytAmount ?? 0,
            DiscountAmount = invoice.DiscountAmount ?? 0,
            PatientAmount = invoice.PatientAmount ?? 0,
            PaymentMethod = invoice.PaymentMethod,
            Status = invoice.Status,
            Notes = invoice.Notes,
            Items = invoice.InvoiceDetails.Select(d => new InvoiceItemViewModel
            {
                ItemName = d.ItemName,
                ItemType = d.ItemType,
                UnitPrice = d.UnitPrice ?? 0,
                Quantity = d.Quantity ?? 0
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Print(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.InvoiceDetails)
            .Include(i => i.Record)
            .ThenInclude(r => r!.Doctor)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice == null) return NotFound();
        return View(invoice);
    }
}

public class ThanhToanViewModel
{
    public int InvoiceId { get; set; }
    public int RecordId { get; set; }
    public int PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientCode { get; set; }
    public string? BhytCode { get; set; }
    public DateTime? VisitDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BhytAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceItemViewModel> Items { get; set; } = new List<InvoiceItemViewModel>();
}

public class InvoiceItemViewModel
{
    public string? ItemName { get; set; }
    public string? ItemType { get; set; }
    public int ItemId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
