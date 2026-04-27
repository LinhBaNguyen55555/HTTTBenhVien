using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Appointment
{
    public int AppId { get; set; }
    public int PatientId { get; set; }
    public int? DoctorId { get; set; }
    public DateTime? AppDate { get; set; }
    public string? TimeSlot { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? Notes { get; set; }
    public int? AppointmentId { get; set; }

    public virtual Employee? Doctor { get; set; }
    public virtual Patient Patient { get; set; } = null!;
}
