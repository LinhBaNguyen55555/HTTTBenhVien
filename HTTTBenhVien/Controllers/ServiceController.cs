using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers
{
    public class ServiceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public ServiceController(HtttbenhvienContext context)
        {
            _context = context;
        }

        public IActionResult TongQuat()
        {
            ViewData["Title"] = "Gói khám tổng quát";
            return View();
        }

        public IActionResult TamSoatUngThu()
        {
            ViewData["Title"] = "Gói tầm soát ung thư";
            return View();
        }

        public async Task<IActionResult> BangGia()
        {
            ViewData["Title"] = "Bảng giá dịch vụ y tế";
            var services = await _context.MedicalServices.OrderBy(s => s.ServiceName).ToListAsync();
            return View(services);
        }
    }
}
