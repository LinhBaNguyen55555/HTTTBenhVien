using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Bed
{
    public int BedId { get; set; }

    public int RoomId { get; set; }

    public string BedNumber { get; set; } = null!;

    public bool? IsOccupied { get; set; }

    public decimal? PricePerDay { get; set; }

    public virtual Room Room { get; set; } = null!;
}
