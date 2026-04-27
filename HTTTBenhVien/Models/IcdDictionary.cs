using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class IcdDictionary
{
    public string IcdCode { get; set; } = null!;

    public string DiseaseName { get; set; } = null!;

    public string? Chapter { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
