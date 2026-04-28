using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class PrescriptionDetail
{
    public int PrescriptionId { get; set; }

    public int MedicineId { get; set; }

    public int? Quantity { get; set; }

    public string? Dosage { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual Prescription Prescription { get; set; } = null!;
}
