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
    public class GiuongController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public GiuongController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/Giuong
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var beds = _context.Beds.Include(b => b.Room).ThenInclude(r => r.Dept).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                beds = beds.Where(b => b.BedNumber.Contains(searchString));
            }

            return View(await beds.ToListAsync());
        }

        // GET: Admin/Giuong/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var bed = await _context.Beds
                .Include(b => b.Room)
                .ThenInclude(r => r.Dept)
                .FirstOrDefaultAsync(m => m.BedId == id);
            
            if (bed == null) return NotFound();

            return View(bed);
        }

        // GET: Admin/Giuong/Create
        public IActionResult Create()
        {
            var rooms = _context.Rooms.Select(r => new {
                RoomId = r.RoomId,
                RoomName = r.RoomNumber + " (Khoa " + r.Dept.DeptName + ")"
            }).ToList();
            
            ViewData["RoomId"] = new SelectList(rooms, "RoomId", "RoomName");
            return View();
        }

        // POST: Admin/Giuong/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BedId,RoomId,BedNumber,IsOccupied,PricePerDay")] Bed bed)
        {
            ModelState.Remove("Room");
            if (ModelState.IsValid)
            {
                // Mặc định giường mới trống
                if (bed.IsOccupied == null) bed.IsOccupied = false;
                
                _context.Add(bed);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm giường mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            var rooms = _context.Rooms.Select(r => new {
                RoomId = r.RoomId,
                RoomName = r.RoomNumber + " (Khoa " + r.Dept.DeptName + ")"
            }).ToList();
            ViewData["RoomId"] = new SelectList(rooms, "RoomId", "RoomName", bed.RoomId);
            return View(bed);
        }

        // GET: Admin/Giuong/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var bed = await _context.Beds.FindAsync(id);
            if (bed == null) return NotFound();
            
            var rooms = _context.Rooms.Select(r => new {
                RoomId = r.RoomId,
                RoomName = r.RoomNumber + " (Khoa " + r.Dept.DeptName + ")"
            }).ToList();
            ViewData["RoomId"] = new SelectList(rooms, "RoomId", "RoomName", bed.RoomId);
            return View(bed);
        }

        // POST: Admin/Giuong/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BedId,RoomId,BedNumber,IsOccupied,PricePerDay")] Bed bed)
        {
            if (id != bed.BedId) return NotFound();

            ModelState.Remove("Room");
            if (ModelState.IsValid)
            {
                try
                {
                    if (bed.IsOccupied == null) bed.IsOccupied = false;
                    _context.Update(bed);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật giường thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BedExists(bed.BedId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            
            var rooms = _context.Rooms.Select(r => new {
                RoomId = r.RoomId,
                RoomName = r.RoomNumber + " (Khoa " + r.Dept.DeptName + ")"
            }).ToList();
            ViewData["RoomId"] = new SelectList(rooms, "RoomId", "RoomName", bed.RoomId);
            return View(bed);
        }

        // GET: Admin/Giuong/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var bed = await _context.Beds
                .Include(b => b.Room)
                .ThenInclude(r => r.Dept)
                .FirstOrDefaultAsync(m => m.BedId == id);
                
            if (bed == null) return NotFound();

            return View(bed);
        }

        // POST: Admin/Giuong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bed = await _context.Beds.FindAsync(id);
            if (bed != null)
            {
                _context.Beds.Remove(bed);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa giường thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BedExists(int id)
        {
            return _context.Beds.Any(e => e.BedId == id);
        }
    }
}
