using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public int DeptId { get; set; }

    public int? RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();

    public virtual Department Dept { get; set; } = null!;

    public virtual RoomType? RoomType { get; set; }
}
