using System.Security.Cryptography;
using System.Text;
using HTTTBenhVien.Areas.Admin.Models;
using HTTTBenhVien.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    private readonly HtttbenhvienContext _context;

    public AccountController(HtttbenhvienContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid)
            return View(model);

        var account = await _context.Accounts
            .Include(a => a.Emp)
            .FirstOrDefaultAsync(a => a.Username == model.Username && a.IsActive == true);

        if (account == null)
        {
            ModelState.AddModelError("", "Tài khoản không tồn tại hoặc đã bị khóa.");
            return View(model);
        }

        if (!VerifyPassword(model.Password, account.PasswordHash))
        {
            ModelState.AddModelError("", "Mật khẩu không đúng.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
            new Claim(ClaimTypes.Name, account.Emp?.FullName ?? account.Username),
            new Claim(ClaimTypes.Role, account.RoleName ?? "Staff"),
            new Claim("EmpId", account.EmpId.ToString()),
            new Claim("Username", account.Username)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(14)
                : DateTimeOffset.UtcNow.AddHours(2)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Register()
    {
        var model = new RegisterViewModel();
        LoadDepartments();
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            LoadDepartments();
            return View(model);
        }

        if (await _context.Accounts.AnyAsync(a => a.Username == model.Username))
        {
            ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
            LoadDepartments();
            return View(model);
        }

        var emp = new Employee
        {
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            DeptId = model.DeptId,
            JobTitle = model.JobTitle
        };
        _context.Employees.Add(emp);
        await _context.SaveChangesAsync();

        var account = new Account
        {
            EmpId = emp.EmpId,
            Username = model.Username,
            PasswordHash = HashPassword(model.Password),
            RoleName = model.RoleName,
            IsActive = true
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo tài khoản cho nhân viên {model.FullName}.";
        return RedirectToAction("Index", "Account");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var accountId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var account = await _context.Accounts.FindAsync(accountId);

        if (account == null)
            return RedirectToAction("Login");

        if (!VerifyPassword(model.CurrentPassword, account.PasswordHash))
        {
            ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
            return View(model);
        }

        account.PasswordHash = HashPassword(model.NewPassword);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đổi mật khẩu thành công.";
        return RedirectToAction("ChangePassword");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var accountId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var account = await _context.Accounts
            .Include(a => a.Emp)
            .ThenInclude(e => e!.Dept)
            .FirstOrDefaultAsync(a => a.AccountId == accountId);

        if (account == null) return RedirectToAction("Login");

        return View(account);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var accounts = await _context.Accounts
            .Include(a => a.Emp)
            .ThenInclude(e => e!.Dept)
            .OrderByDescending(a => a.AccountId)
            .ToListAsync();

        return View(accounts);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var account = await _context.Accounts
            .Include(a => a.Emp)
            .FirstOrDefaultAsync(a => a.AccountId == id);

        if (account == null)
            return NotFound();

        var model = new RegisterViewModel
        {
            Username = account.Username,
            FullName = account.Emp?.FullName ?? "",
            Email = account.Emp?.Email ?? "",
            Phone = account.Emp?.Phone ?? "",
            RoleName = account.RoleName ?? "Staff",
            DeptId = account.Emp?.DeptId,
            JobTitle = account.Emp?.JobTitle
        };

        ViewBag.AccountId = id;
        ViewBag.IsActive = account.IsActive;
        LoadDepartments();
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RegisterViewModel model, bool IsActive)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AccountId = id;
            ViewBag.IsActive = IsActive;
            LoadDepartments();
            return View(model);
        }

        var account = await _context.Accounts
            .Include(a => a.Emp)
            .FirstOrDefaultAsync(a => a.AccountId == id);

        if (account == null)
            return NotFound();

        if (await _context.Accounts.AnyAsync(a => a.Username == model.Username && a.AccountId != id))
        {
            ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
            ViewBag.AccountId = id;
            ViewBag.IsActive = IsActive;
            LoadDepartments();
            return View(model);
        }

        account.Username = model.Username;
        account.RoleName = model.RoleName;
        account.IsActive = IsActive;

        if (account.Emp != null)
        {
            account.Emp.FullName = model.FullName;
            account.Emp.Email = model.Email;
            account.Emp.Phone = model.Phone;
            account.Emp.DeptId = model.DeptId;
            account.Emp.JobTitle = model.JobTitle;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật tài khoản thành công.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var account = await _context.Accounts
            .Include(a => a.Emp)
            .FirstOrDefaultAsync(a => a.AccountId == id);

        if (account == null)
            return NotFound();

        account.PasswordHash = HashPassword("123456");
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã đặt lại mật khẩu cho {account.Emp?.FullName ?? account.Username} thành '123456'.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var account = await _context.Accounts
            .Include(a => a.Emp)
            .FirstOrDefaultAsync(a => a.AccountId == id);

        if (account == null)
            return NotFound();

        account.IsActive = false;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã khóa tài khoản của {account.Emp?.FullName ?? account.Username}.";
        return RedirectToAction("Index");
    }

    private void LoadDepartments()
    {
        var depts = _context.Departments.OrderBy(d => d.DeptName).ToList();
        ViewBag.Departments = depts;
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "HTTTBenhVien_Salt_2024"));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
