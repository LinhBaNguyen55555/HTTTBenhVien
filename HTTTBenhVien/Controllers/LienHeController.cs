using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers;

public class LienHeController : Controller
{
    private readonly HtttbenhvienContext _context;

    public LienHeController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Contact model)
    {
        if (!ModelState.IsValid)
            return View(model);

        model.Status = "New";
        model.SubmitDate = DateTime.Now;
        _context.Contacts.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
        return RedirectToAction("Index");
    }
}
