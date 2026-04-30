using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class RecordService
{
    public int RsId { get; set; }

    public int RecordId { get; set; }

    public int ServiceId { get; set; }

    public int DoctorId { get; set; }

    public string? ResultValue { get; set; }

    public DateTime? ExecuteDate { get; set; }

    public string? Status { get; set; }

    public string? ResultImages { get; set; }

    public virtual Employee Doctor { get; set; } = null!;

    public virtual MedicalRecord Record { get; set; } = null!;

    public virtual MedicalService Service { get; set; } = null!;
}
