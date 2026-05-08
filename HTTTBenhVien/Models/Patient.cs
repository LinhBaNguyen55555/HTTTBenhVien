using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HTTTBenhVien.Models;

public partial class Patient
{
    public int PatientId { get; set; }
    public string? PatientCode { get; set; }
    public string FullName { get; set; } = null!;
    public DateTime? Dob { get; set; }
    public bool Gender { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? BhytCode { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }

    [NotMapped]
    public string AvatarUrlFull
    {
        get
        {
            const string basePath = "/assets/img/imagepatient/";
            const string defaultImage = "default-patient.jpg";
            return basePath + (string.IsNullOrEmpty(this.AvatarUrl) ? defaultImage : this.AvatarUrl);
        }
    }

    public string? Notes { get; set; }

    public string? CitizenId { get; set; }
    public string? FamilyMedicalHistory { get; set; }
    public string? PersonalMedicalHistory { get; set; }
    public string? Occupation { get; set; }
    public string? WorkplaceOrSchool { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public virtual PatientAccount? PatientAccount { get; set; }
}
