using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class MedicalSupply
{
    public int SupplyId { get; set; }

    public string SupplyName { get; set; } = null!;

    public string? Unit { get; set; }

    public int StockQuantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? Supplier { get; set; }

    public bool? IsActive { get; set; }
}
