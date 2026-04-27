using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class WebMenu
{
    public int MenuId { get; set; }

    public string MenuName { get; set; } = null!;

    public string MenuUrl { get; set; } = null!;

    public int? ParentId { get; set; }

    public int? DisplayOrder { get; set; }

    public string? IconClass { get; set; }

    public string? RoleRequired { get; set; }

    public virtual ICollection<WebMenu> InverseParent { get; set; } = new List<WebMenu>();

    public virtual WebMenu? Parent { get; set; }
}
