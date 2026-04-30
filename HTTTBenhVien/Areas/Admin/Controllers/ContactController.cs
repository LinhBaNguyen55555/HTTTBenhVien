using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ContactController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public ContactController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/Contact
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            var contacts = _context.Contacts
                .Include(c => c.ResolvedByNavigation)
                .OrderByDescending(c => c.SubmitDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                contacts = contacts.Where(c =>
                    c.FullName.Contains(searchString) ||
                    c.Subject.Contains(searchString) ||
                    (c.Email != null && c.Email.Contains(searchString)) ||
                    c.Phone.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                contacts = contacts.Where(c => c.Status == statusFilter);
            }

            return View(await contacts.ToListAsync());
        }

        // GET: Admin/Contact/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var contact = await _context.Contacts
                .Include(c => c.ResolvedByNavigation)
                .FirstOrDefaultAsync(m => m.ContactId == id);

            if (contact == null) return NotFound();

            ViewData["ResolvedBy"] = new SelectList(_context.Employees, "EmpId", "FullName", contact.ResolvedBy);
            return View(contact);
        }

        // POST: Admin/Contact/Resolve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int ContactId, string Status, int? ResolvedBy, string? ResolveNote)
        {
            var contact = await _context.Contacts.FindAsync(ContactId);
            if (contact == null) return NotFound();

            contact.Status = Status;
            contact.ResolvedBy = ResolvedBy;
            contact.ResolveNote = ResolveNote;

            if (Status == "DaXuLy" && contact.ResolvedDate == null)
                contact.ResolvedDate = DateTime.Now;

            _context.Update(contact);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật trạng thái liên hệ thành công!";
            return RedirectToAction(nameof(Details), new { id = ContactId });
        }

        // GET: Admin/Contact/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var contact = await _context.Contacts
                .Include(c => c.ResolvedByNavigation)
                .FirstOrDefaultAsync(m => m.ContactId == id);

            if (contact == null) return NotFound();
            return View(contact);
        }

        // POST: Admin/Contact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa liên hệ thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
