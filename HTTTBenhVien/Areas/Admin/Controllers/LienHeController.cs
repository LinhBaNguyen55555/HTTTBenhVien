using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class LienHeController : Controller
{
    private readonly HtttbenhvienContext _context;

    public LienHeController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var query = _context.Contacts.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        ViewBag.Status = status;
        ViewBag.NewCount = await _context.Contacts.CountAsync(c => c.Status == "New");
        ViewBag.RepliedCount = await _context.Contacts.CountAsync(c => c.Status == "Replied");

        var contacts = await query.OrderByDescending(c => c.SubmitDate).ToListAsync();
        return View(contacts);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null) return NotFound();

        if (contact.Status == "New")
        {
            contact.Status = "DaDoc";
            await _context.SaveChangesAsync();
        }

        return View(contact);
    }

    [HttpGet]
    public async Task<IActionResult> Reply(int id, string reply)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null) return NotFound();

        contact.Status = "Replied";
        contact.ResolveNote = reply;
        contact.ResolvedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Da gui phan hoi lien he.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null) return NotFound();

        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Da xoa lien he.";
        return RedirectToAction("Index");
    }
}
