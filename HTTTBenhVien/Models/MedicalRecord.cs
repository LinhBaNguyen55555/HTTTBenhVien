using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class MedicalRecord
{
    public int RecordId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int? AppointmentId { get; set; }
    public DateTime? VisitDate { get; set; }
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? Conclusion { get; set; }
    public string? RecordType { get; set; }
    public string? IcdCode { get; set; }
    public string? Notes { get; set; }
    public string? VitalSigns { get; set; }

    public string? BloodPressure { get; set; }
    public int? PulseRate { get; set; }
    public decimal? Temperature { get; set; }
    public int? Height { get; set; }
    public decimal? Weight { get; set; }
    public int? SpO2 { get; set; }

    public virtual Employee Doctor { get; set; } = null!;
    public virtual IcdDictionary? IcdCodeNavigation { get; set; }
    public virtual Invoice? Invoice { get; set; }
    public virtual Patient Patient { get; set; } = null!;
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public virtual ICollection<RecordService> RecordServices { get; set; } = new List<RecordService>();
}
