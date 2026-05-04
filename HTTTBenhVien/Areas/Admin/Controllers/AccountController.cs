using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public AccountController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // Helper: Hash password (without salt - for new accounts)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // Helper: Hash password (with salt - for seed data compatibility)
        private string HashPasswordWithSalt(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "HTTTBenhVien_Salt_2024"));
            return Convert.ToBase64String(bytes);
        }

        // =============================================
        // LOGIN / LOGOUT
        // =============================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already logged in, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            var hashedPassword = HashPassword(password);
            var hashedPasswordWithSalt = HashPasswordWithSalt(password);

            // Find account by username, then check password (support multiple formats)
            var account = await _context.Accounts
                .Include(a => a.Emp)
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null || 
                (account.Password != hashedPassword && 
                 account.Password != hashedPasswordWithSalt && 
                 account.Password != password))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                ViewBag.Username = username;
                return View();
            }

            if (account.IsActive != true)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                ViewBag.Username = username;
                return View();
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Name, account.Emp?.FullName ?? account.Username),
                new Claim(ClaimTypes.Role, account.RoleName ?? "User"),
                new Claim("AccountId", account.AccountId.ToString()),
                new Claim("EmpId", account.EmpId.ToString()),
                new Claim("Username", account.Username),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =============================================
        // CRUD - Account Management
        // =============================================

        // GET: Admin/Account
        public async Task<IActionResult> Index(string searchString, string roleFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["RoleFilter"] = roleFilter;

            var accounts = _context.Accounts
                .Include(a => a.Emp)
                .ThenInclude(e => e.Dept)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                accounts = accounts.Where(a =>
                    a.Username.Contains(searchString) ||
                    (a.Emp != null && a.Emp.FullName.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                accounts = accounts.Where(a => a.RoleName == roleFilter);
            }

            return View(await accounts.OrderByDescending(a => a.AccountId).ToListAsync());
        }

        // GET: Admin/Account/Create
        public IActionResult Create()
        {
            var empWithAccount = _context.Accounts.Select(a => a.EmpId).ToList();
            var availableEmps = _context.Employees
                .Where(e => !empWithAccount.Contains(e.EmpId))
                .Select(e => new { e.EmpId, Display = e.FullName + " - " + (e.JobTitle ?? "N/A") })
                .ToList();

            ViewData["EmpId"] = new SelectList(availableEmps, "EmpId", "Display");
            return View();
        }

        // POST: Admin/Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Account account, string Password)
        {
            ModelState.Remove("Emp");
            ModelState.Remove("Password");

            if (string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
            }

            if (await _context.Accounts.AnyAsync(a => a.Username == account.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                account.Password = HashPassword(Password);
                if (account.IsActive == null) account.IsActive = true;

                _context.Add(account);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo tài khoản thành công!";
                return RedirectToAction(nameof(Index));
            }

            var empWithAccount = _context.Accounts.Select(a => a.EmpId).ToList();
            var availableEmps = _context.Employees
                .Where(e => !empWithAccount.Contains(e.EmpId))
                .Select(e => new { e.EmpId, Display = e.FullName + " - " + (e.JobTitle ?? "N/A") })
                .ToList();
            ViewData["EmpId"] = new SelectList(availableEmps, "EmpId", "Display", account.EmpId);
            return View(account);
        }

        // GET: Admin/Account/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.Accounts.Include(a => a.Emp).FirstOrDefaultAsync(a => a.AccountId == id);
            if (account == null) return NotFound();

            var empList = _context.Employees
                .Select(e => new { e.EmpId, Display = e.FullName + " - " + (e.JobTitle ?? "N/A") })
                .ToList();
            ViewData["EmpId"] = new SelectList(empList, "EmpId", "Display", account.EmpId);
            return View(account);
        }

        // POST: Admin/Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Account account, string? NewPassword)
        {
            if (id != account.AccountId) return NotFound();

            ModelState.Remove("Emp");
            ModelState.Remove("Password");
            ModelState.Remove("NewPassword");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAccount = await _context.Accounts.FindAsync(id);
                    if (existingAccount == null) return NotFound();

                    existingAccount.Username = account.Username;
                    existingAccount.EmpId = account.EmpId;
                    existingAccount.RoleName = account.RoleName;
                    existingAccount.IsActive = account.IsActive;

                    if (!string.IsNullOrEmpty(NewPassword))
                    {
                        existingAccount.Password = HashPassword(NewPassword);
                    }

                    _context.Update(existingAccount);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Accounts.Any(e => e.AccountId == id)) return NotFound();
                    else throw;
                }
            }

            var empList = _context.Employees
                .Select(e => new { e.EmpId, Display = e.FullName + " - " + (e.JobTitle ?? "N/A") })
                .ToList();
            ViewData["EmpId"] = new SelectList(empList, "EmpId", "Display", account.EmpId);
            return View(account);
        }

        // POST: Admin/Account/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            account.IsActive = !(account.IsActive ?? true);
            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["Success"] = account.IsActive == true
                ? $"Đã kích hoạt tài khoản \"{account.Username}\"."
                : $"Đã khóa tài khoản \"{account.Username}\".";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Account/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.Accounts
                .Include(a => a.Emp)
                .FirstOrDefaultAsync(m => m.AccountId == id);

            if (account == null) return NotFound();
            return View(account);
        }

        // POST: Admin/Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account != null)
            {
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa tài khoản thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
