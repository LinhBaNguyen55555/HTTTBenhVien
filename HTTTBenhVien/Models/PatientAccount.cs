using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class PatientAccount
{
    public int AccId { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int PatientId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
