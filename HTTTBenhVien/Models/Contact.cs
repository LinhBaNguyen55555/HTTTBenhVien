using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Contact
{
    public int ContactId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string Phone { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string MessageContent { get; set; } = null!;

    public DateTime? SubmitDate { get; set; }

    public string? Status { get; set; }

    public int? ResolvedBy { get; set; }

    public string? ResolveNote { get; set; }

    public DateTime? ResolvedDate { get; set; }

    public virtual Employee? ResolvedByNavigation { get; set; }
}
