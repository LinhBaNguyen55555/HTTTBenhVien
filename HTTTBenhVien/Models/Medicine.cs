using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Medicine
{
    public int MedicineId { get; set; }
    public string? MedicineCode { get; set; }
    public string MedicineName { get; set; } = null!;
    public string? ActiveIngredient { get; set; }
    public string? Unit { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }

    public virtual ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();
}
