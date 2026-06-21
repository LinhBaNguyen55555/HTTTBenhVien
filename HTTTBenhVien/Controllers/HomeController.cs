using System.Diagnostics;
using HTTTBenhVien.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HTTTBenhVien.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HtttbenhvienContext _context;

        public HomeController(ILogger<HomeController> logger, HtttbenhvienContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Employees
                .Include(e => e.Dept)
                .Where(e => e.JobTitle != null && e.JobTitle.Contains("Bác sĩ"))
                .OrderBy(e => e.FullName)
                .Take(6)
                .ToListAsync();

            var departments = await _context.Departments
                .Include(d => d.DeptTypeNavigation)
                .Where(d => d.DeptTypeNavigation != null && d.DeptTypeNavigation.DeptTypeName == "Lâm sàng")
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            ViewBag.FeaturedDoctors = doctors;
            ViewBag.FeaturedDepartments = departments;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
