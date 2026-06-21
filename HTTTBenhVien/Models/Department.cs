using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HTTTBenhVien.Models;

public partial class Department
{
    public int DeptId { get; set; }
    public string? DeptName { get; set; }
    public int? DeptTypeId { get; set; }
    public string? Location { get; set; }

    [NotMapped]
    public string? Description { get; set; }

    public virtual DeptType? DeptTypeNavigation { get; set; }
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
