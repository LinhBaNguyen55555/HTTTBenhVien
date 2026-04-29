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
    public class WebMenuController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public WebMenuController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/WebMenu
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var menus = _context.WebMenus.Include(w => w.Parent).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                menus = menus.Where(m => m.MenuName.Contains(searchString));
            }

            menus = menus.OrderBy(w => w.DisplayOrder);
            
            return View(await menus.ToListAsync());
        }

        // GET: Admin/WebMenu/Create
        public IActionResult Create()
        {
            ViewData["ParentId"] = new SelectList(_context.WebMenus.Where(m => m.ParentId == null), "MenuId", "MenuName");
            return View();
        }

        // POST: Admin/WebMenu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MenuId,MenuName,MenuUrl,ParentId,DisplayOrder,IconClass,RoleRequired")] WebMenu webMenu)
        {
            ModelState.Remove("Parent");
            ModelState.Remove("InverseParent");
            if (ModelState.IsValid)
            {
                _context.Add(webMenu);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm menu mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentId"] = new SelectList(_context.WebMenus.Where(m => m.ParentId == null), "MenuId", "MenuName", webMenu.ParentId);
            return View(webMenu);
        }

        // GET: Admin/WebMenu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var webMenu = await _context.WebMenus.FindAsync(id);
            if (webMenu == null) return NotFound();

            ViewData["ParentId"] = new SelectList(_context.WebMenus.Where(m => m.ParentId == null && m.MenuId != id), "MenuId", "MenuName", webMenu.ParentId);
            return View(webMenu);
        }

        // POST: Admin/WebMenu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MenuId,MenuName,MenuUrl,ParentId,DisplayOrder,IconClass,RoleRequired")] WebMenu webMenu)
        {
            if (id != webMenu.MenuId) return NotFound();

            ModelState.Remove("Parent");
            ModelState.Remove("InverseParent");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(webMenu);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật menu thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WebMenuExists(webMenu.MenuId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentId"] = new SelectList(_context.WebMenus.Where(m => m.ParentId == null && m.MenuId != id), "MenuId", "MenuName", webMenu.ParentId);
            return View(webMenu);
        }

        // GET: Admin/WebMenu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var webMenu = await _context.WebMenus
                .Include(w => w.Parent)
                .FirstOrDefaultAsync(m => m.MenuId == id);
            
            if (webMenu == null) return NotFound();

            return View(webMenu);
        }

        // POST: Admin/WebMenu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var webMenu = await _context.WebMenus.FindAsync(id);
            if (webMenu != null)
            {
                _context.WebMenus.Remove(webMenu);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa menu thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool WebMenuExists(int id)
        {
            return _context.WebMenus.Any(e => e.MenuId == id);
        }
    }
}
