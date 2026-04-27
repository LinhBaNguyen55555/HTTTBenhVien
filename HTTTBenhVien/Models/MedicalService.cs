using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class MedicalService
{
    public int ServiceId { get; set; }

    public string ServiceName { get; set; } = null!;

    public decimal BasePrice { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<RecordService> RecordServices { get; set; } = new List<RecordService>();
}
