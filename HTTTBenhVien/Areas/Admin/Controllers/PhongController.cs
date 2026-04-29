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
    public class PhongController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public PhongController(HtttbenhvienContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var rooms = _context.Rooms.Include(r => r.Dept).Include(r => r.RoomType).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                rooms = rooms.Where(r => r.RoomNumber.Contains(searchString));
            }

            return View(await rooms.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.Dept)
                .Include(r => r.RoomType)
                .Include(r => r.Beds)
                .FirstOrDefaultAsync(m => m.RoomId == id);
                
            if (room == null) return NotFound();

            return View(room);
        }

        public IActionResult Create()
        {
            ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName");
            ViewData["RoomTypeId"] = new SelectList(_context.RoomTypes, "RoomTypeId", "RoomTypeName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,DeptId,RoomTypeId,RoomNumber")] Room room)
        {
            ModelState.Remove("Dept");
            ModelState.Remove("RoomType");
            ModelState.Remove("Beds");
            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm phòng mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName", room.DeptId);
            ViewData["RoomTypeId"] = new SelectList(_context.RoomTypes, "RoomTypeId", "RoomTypeName", room.RoomTypeId);
            return View(room);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            
            ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName", room.DeptId);
            ViewData["RoomTypeId"] = new SelectList(_context.RoomTypes, "RoomTypeId", "RoomTypeName", room.RoomTypeId);
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomId,DeptId,RoomTypeId,RoomNumber")] Room room)
        {
            if (id != room.RoomId) return NotFound();

            ModelState.Remove("Dept");
            ModelState.Remove("RoomType");
            ModelState.Remove("Beds");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật phòng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName", room.DeptId);
            ViewData["RoomTypeId"] = new SelectList(_context.RoomTypes, "RoomTypeId", "RoomTypeName", room.RoomTypeId);
            return View(room);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.Dept)
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(m => m.RoomId == id);
                
            if (room == null) return NotFound();

            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa phòng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
        }
    }
}
