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
    public class KhoaController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public KhoaController(HtttbenhvienContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var departments = _context.Departments
                .Include(d => d.DeptTypeNavigation)
                .Include(d => d.Employees)
                .Include(d => d.Rooms).ThenInclude(r => r.RoomType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                departments = departments.Where(d => d.DeptName.Contains(searchString));
            }

            return View(await departments.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.DeptTypeId = new SelectList(_context.DeptTypes, "DeptTypeId", "DeptTypeName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DeptName,DeptTypeId,Location,Description")] Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm khoa mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.DeptTypeId = new SelectList(_context.DeptTypes, "DeptTypeId", "DeptTypeName", department.DeptTypeId);
            return View(department);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            ViewBag.DeptTypeId = new SelectList(_context.DeptTypes, "DeptTypeId", "DeptTypeName", department.DeptTypeId);
            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DeptId,DeptName,DeptTypeId,Location,Description")] Department department)
        {
            if (id != department.DeptId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật khoa thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DeptId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.DeptTypeId = new SelectList(_context.DeptTypes, "DeptTypeId", "DeptTypeName", department.DeptTypeId);
            return View(department);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var department = await _context.Departments
                .Include(d => d.DeptTypeNavigation)
                .Include(d => d.Employees)
                .Include(d => d.Rooms).ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(m => m.DeptId == id);

            if (department == null)
                return NotFound();

            return View(department);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var department = await _context.Departments
                .Include(d => d.DeptTypeNavigation)
                .Include(d => d.Employees)
                .Include(d => d.Rooms).ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(m => m.DeptId == id);

            if (department == null)
                return NotFound();

            return View(department);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa khoa thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id) => _context.Departments.Any(e => e.DeptId == id);
    }
}
