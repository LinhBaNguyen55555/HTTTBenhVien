using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EmployeeController : Controller
    {
        private readonly HtttbenhvienContext _context;
        private readonly IWebHostEnvironment _env;

        public EmployeeController(HtttbenhvienContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/NhanVien
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var employees = _context.Employees.Include(e => e.Dept).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e => 
                    e.FullName.Contains(searchString) || 
                    e.JobTitle.Contains(searchString));
            }

            return View(await employees.ToListAsync());
        }

        // GET: Admin/NhanVien/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Dept)
                .FirstOrDefaultAsync(m => m.EmpId == id);
                
            if (employee == null) return NotFound();

            return View(employee);
        }

        // GET: Admin/NhanVien/Create
        public IActionResult Create()
        {
            ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName");
            return View();
        }

        // POST: Admin/NhanVien/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmpId,DeptId,FullName,JobTitle,Specialty,Email,Phone,Gender,AcademicTitle")] Employee employee, IFormFile? AvatarUpload)
        {
            ModelState.Remove("Dept");
            ModelState.Remove("Account");
            ModelState.Remove("Appointments");
            ModelState.Remove("MedicalRecords");
            ModelState.Remove("Articles");
            ModelState.Remove("Contacts");

            if (ModelState.IsValid)
            {
                if (AvatarUpload != null && AvatarUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "assets", "img", "imagedoctor");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(AvatarUpload.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await AvatarUpload.CopyToAsync(fileStream);
                    }
                    employee.AvatarUrl = uniqueFileName;
                }

                _context.Add(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm nhân viên mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName", employee.DeptId);
            return View(employee);
        }

        // GET: Admin/NhanVien/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName", employee.DeptId);
            return View(employee);
        }

        // POST: Admin/NhanVien/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmpId,DeptId,FullName,JobTitle,Specialty,Email,Phone,AvatarUrl,Gender,AcademicTitle")] Employee employee, IFormFile? AvatarUpload, bool RemoveAvatar = false)
        {
            if (id != employee.EmpId) return NotFound();

            ModelState.Remove("Dept");
            ModelState.Remove("Account");
            ModelState.Remove("Appointments");
            ModelState.Remove("MedicalRecords");
            ModelState.Remove("Articles");
            ModelState.Remove("Contacts");

            if (ModelState.IsValid)
            {
                try
                {
                    if (RemoveAvatar)
                    {
                        employee.AvatarUrl = null;
                    }

                    if (AvatarUpload != null && AvatarUpload.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_env.WebRootPath, "assets", "img", "imagedoctor");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(AvatarUpload.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await AvatarUpload.CopyToAsync(fileStream);
                        }
                        
                        employee.AvatarUrl = uniqueFileName;
                    }

                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật nhân viên thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmpId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName", employee.DeptId);
            return View(employee);
        }

        // GET: Admin/NhanVien/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Dept)
                .FirstOrDefaultAsync(m => m.EmpId == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Admin/NhanVien/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa nhân viên thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmpId == id);
        }
    }
}

