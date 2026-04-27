using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Patient
{
    public int PatientId { get; set; }
    public string? PatientCode { get; set; }
    public string FullName { get; set; } = null!;
    public DateTime? Dob { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? BhytCode { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Notes { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
