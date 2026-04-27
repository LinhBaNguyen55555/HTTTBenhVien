using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }
    public int PatientId { get; set; }
    public int? RecordId { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? BhytAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? PatientAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    public virtual Patient Patient { get; set; } = null!;
    public virtual MedicalRecord? Record { get; set; }
}
