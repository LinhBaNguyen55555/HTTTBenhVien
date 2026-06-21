using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers;

public class AccountController : Controller
{
    private readonly HtttbenhvienContext _context;

    public AccountController(HtttbenhvienContext context)
    {
        _context = context;
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(PatientRegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (await _context.PatientAccounts.AnyAsync(a => a.PhoneNumber == model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được đăng ký tài khoản.");
                    return View(model);
                }

                var existingPatient = await _context.Patients.FirstOrDefaultAsync(p => p.Phone == model.PhoneNumber);
                
                if (existingPatient == null)
                {
                    existingPatient = new Patient
                    {
                        FullName = model.FullName,
                        Phone = model.PhoneNumber,
                        PatientCode = "BN" + DateTime.Now.ToString("yyMMddHHmmss"),
                        Dob = new DateTime(2000, 1, 1) 
                    };
                    _context.Patients.Add(existingPatient);
                    await _context.SaveChangesAsync();
                }

                var account = new PatientAccount
                {
                    PhoneNumber = model.PhoneNumber,
                    Password = HashPassword(model.Password),
                    PatientId = existingPatient.PatientId,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.PatientAccounts.Add(account);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống khi đăng ký: " + ex.InnerException?.Message ?? ex.Message);
            }
        }
        else
        {
            ModelState.AddModelError("", "Vui lòng kiểm tra lại thông tin nhập vào.");
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(PatientLoginViewModel model, string? returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            var hashedPassword = HashPassword(model.Password);
            var account = await _context.PatientAccounts
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.PhoneNumber == model.PhoneNumber && (a.Password == hashedPassword || a.Password == model.Password));

            if (account == null)
            {
                ModelState.AddModelError("", "Số điện thoại hoặc mật khẩu không chính xác.");
                return View(model);
            }

            if (account.IsActive != true)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đang bị tạm khóa.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccId.ToString()),
                new Claim(ClaimTypes.Name, account.Patient.FullName),
                new Claim(ClaimTypes.Role, "Patient"),
                new Claim("PatientId", account.PatientId.ToString()),
                new Claim("PhoneNumber", account.PhoneNumber)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
