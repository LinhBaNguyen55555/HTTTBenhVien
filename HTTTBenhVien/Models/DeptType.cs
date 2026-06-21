using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class DeptType
{
    public int DeptTypeId { get; set; }

    public string DeptTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}
