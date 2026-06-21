using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public int EmpId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? RoleName { get; set; }

    public bool? IsActive { get; set; }

    public virtual Employee Emp { get; set; } = null!;
}
