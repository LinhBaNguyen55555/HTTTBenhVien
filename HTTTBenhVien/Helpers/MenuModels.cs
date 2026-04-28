namespace HTTTBenhVien.Helpers;

/// <summary>
/// Định nghĩa cấu trúc một mục menu trong sidebar
/// </summary>
public class MenuItem
{
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Url { get; set; } = "#";
    public string? Badge { get; set; }
    public string? BadgeClass { get; set; }
    public List<MenuItem> Children { get; set; } = new();
    public bool IsDivider { get; set; }
    public bool IsHeader { get; set; }
    public string? HeaderText { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string? DividerClass { get; set; }
}

/// <summary>
/// Định nghĩa cấu trúc một workspace
/// </summary>
public class WorkspaceInfo
{
    public string Role { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Description { get; set; } = "";
    public string Color { get; set; } = "#175cdd";
    public string Url { get; set; } = "/";
}
