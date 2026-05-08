using Microsoft.AspNetCore.Mvc;

namespace HTTTBenhVien.Controllers
{
    public class GuideController : Controller
    {
        public IActionResult QuyTrinh()
        {
            ViewData["Title"] = "Quy trình khám bệnh";
            return View();
        }

        public IActionResult BaoHiem()
        {
            ViewData["Title"] = "Hướng dẫn bảo hiểm y tế";
            return View();
        }

        public IActionResult ThamNuoi()
        {
            ViewData["Title"] = "Quy định thăm nuôi bệnh nhân";
            return View();
        }
    }
}
