using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class InvoiceDetail
{
    public int InvDetailId { get; set; }

    public int InvoiceId { get; set; }

    public string? ItemType { get; set; }

    public int? ItemId { get; set; }

    public string? ItemName { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
