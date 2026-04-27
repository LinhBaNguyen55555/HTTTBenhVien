namespace HTTTBenhVien.Areas.Admin.Models;

public class LoginViewModel
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public int? DeptId { get; set; }
    public string? JobTitle { get; set; }
}

public class ChangePasswordViewModel
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
