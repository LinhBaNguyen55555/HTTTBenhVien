using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers;

public class ChuyenKhoaController : Controller
{
    private readonly HtttbenhvienContext _context;

    public ChuyenKhoaController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var allDepts = await _context.Departments
            .Include(d => d.DeptTypeNavigation)
            .OrderBy(d => d.DeptTypeNavigation.DeptTypeName)
            .ThenBy(d => d.DeptName)
            .ToListAsync();

        var vm = new ChuyenKhoaViewModel
        {
            KhoaLamSang = allDepts.Where(d => string.Equals(d.DeptTypeNavigation?.DeptTypeName?.Trim(), "Lâm sàng", StringComparison.OrdinalIgnoreCase)).ToList(),
            KhoaCanLamSang = allDepts.Where(d => string.Equals(d.DeptTypeNavigation?.DeptTypeName?.Trim(), "Cận lâm sàng", StringComparison.OrdinalIgnoreCase)).ToList(),
            KhoaCapCuu = allDepts.Where(d => string.Equals(d.DeptTypeNavigation?.DeptTypeName?.Trim(), "Cấp cứu", StringComparison.OrdinalIgnoreCase)).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var dept = await _context.Departments
            .Include(d => d.DeptTypeNavigation)
            .Include(d => d.Employees)
            .ThenInclude(e => e.Account)
            .FirstOrDefaultAsync(d => d.DeptId == id);

        if (dept == null)
            return NotFound();

        var services = await _context.MedicalServices
            .ToListAsync();

        // Lọc dịch vụ theo khoa
        var deptName = dept.DeptName?.Trim() ?? "";
        List<MedicalService> relatedServices;
        string serviceSectionTitle;

        if (deptName.IndexOf("Xét", StringComparison.OrdinalIgnoreCase) >= 0 &&
            deptName.IndexOf("nghiệm", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Khoa Xét nghiệm
            relatedServices = services
                .Where(s => s.ServiceName != null && (
                    s.ServiceName.Contains("Xét nghiệm")))
                .ToList();
            serviceSectionTitle = "Bảng giá xét nghiệm";
        }
        else if (deptName.IndexOf("Chẩn", StringComparison.OrdinalIgnoreCase) >= 0 &&
                 deptName.IndexOf("hình", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Khoa Chẩn đoán hình ảnh
            relatedServices = services
                .Where(s => s.ServiceName != null && (
                    s.ServiceName.Contains("Siêu âm") ||
                    s.ServiceName.Contains("X-Quang") ||
                    s.ServiceName.Contains("Chụp CT") ||
                    s.ServiceName.Contains("Chụp MRI") ||
                    s.ServiceName.Contains("Nội soi") ||
                    s.ServiceName.Contains("Điện Tâm") ||
                    s.ServiceName.Contains("Điện Não")))
                .ToList();
            serviceSectionTitle = "Bảng giá dịch vụ chẩn đoán";
        }
        else if (deptName.IndexOf("Dược", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Khoa Dược - hiển thị tất cả dịch vụ
            relatedServices = services.ToList();
            serviceSectionTitle = "Dịch vụ y tế liên quan";
        }
        else
        {
            // Các khoa khác - hiển thị tất cả
            relatedServices = services.ToList();
            serviceSectionTitle = "Dịch vụ y tế liên quan";
        }

        ViewBag.Services = relatedServices;
        ViewBag.ServiceSectionTitle = serviceSectionTitle;

        return View(dept);
    }
}
