using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public int RecordId { get; set; }

    public int DoctorId { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// Trạng thái đơn thuốc: ChoCapPhat, DaCapPhat, Huy
    /// </summary>
    public string? Status { get; set; }

    public virtual ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();

    public virtual MedicalRecord Record { get; set; } = null!;
}
