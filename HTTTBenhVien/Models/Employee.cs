using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HTTTBenhVien.Models;

public partial class Employee
{
    public int EmpId { get; set; }
    public int? DeptId { get; set; }
    public string? FullName { get; set; }
    public string? JobTitle { get; set; }
    public string? Specialty { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Cột AvatarUrl trong SQL (lưu tên ảnh, VD: doctor1.jpg)
    public string? AvatarUrl { get; set; }

    // Computed property trả về đường dẫn đầy đủ
    [NotMapped]
    public string AvatarUrlFull
    {
        get
        {
            const string basePath = "/assets/img/imagedoctor/";
            const string defaultImage = "default-avatar.jpg";
            return basePath + (string.IsNullOrEmpty(this.AvatarUrl) ? defaultImage : this.AvatarUrl);
        }
    }

    [NotMapped]
    public string? Address { get; set; }

    [NotMapped]
    public DateTime? Dob { get; set; }

    [NotMapped]
    public string? Gender { get; set; }

    public virtual Account? Account { get; set; }
    public virtual Department? Dept { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
