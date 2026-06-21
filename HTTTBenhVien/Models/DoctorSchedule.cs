using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class DoctorSchedule
{
    public int ScheduleId { get; set; }

    public int DoctorId { get; set; }

    public DateTime WorkDate { get; set; }

    public string ShiftType { get; set; } = null!;

    public int? MaxAppointments { get; set; }

    public bool? IsAvailable { get; set; }

    public string? Notes { get; set; }

    public virtual Employee Doctor { get; set; } = null!;

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
