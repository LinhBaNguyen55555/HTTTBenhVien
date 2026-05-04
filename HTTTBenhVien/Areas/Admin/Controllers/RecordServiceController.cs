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
    public class RecordServiceController : Controller
    {
        private readonly HtttbenhvienContext _context;

        public RecordServiceController(HtttbenhvienContext context)
        {
            _context = context;
        }

        // GET: Admin/RecordService
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            var records = _context.RecordServices
                .Include(r => r.Record).ThenInclude(rec => rec.Patient)
                .Include(r => r.Service)
                .Include(r => r.Doctor)
                .OrderByDescending(r => r.ExecuteDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                records = records.Where(r =>
                    r.Record.Patient.FullName.Contains(searchString) ||
                    r.Service.ServiceName.Contains(searchString) ||
                    (r.Doctor != null && r.Doctor.FullName.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                records = records.Where(r => r.Status == statusFilter);
            }

            return View(await records.ToListAsync());
        }

        // GET: Admin/RecordService/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var rs = await _context.RecordServices
                .Include(r => r.Record).ThenInclude(rec => rec.Patient)
                .Include(r => r.Service)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(m => m.RsId == id);

            if (rs == null) return NotFound();

            return View(rs);
        }

        // GET: Admin/RecordService/Create
        public IActionResult Create()
        {
            // Build a list showing Patient name + RecordId
            var recordList = _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.RecordId, Display = "BA #" + m.RecordId + " - " + m.Patient.FullName })
                .ToList();

            ViewData["RecordId"] = new SelectList(recordList, "RecordId", "Display");
            ViewData["ServiceId"] = new SelectList(_context.MedicalServices, "ServiceId", "ServiceName");
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName");
            return View();
        }

        // POST: Admin/RecordService/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RsId,RecordId,ServiceId,DoctorId,ResultValue,ExecuteDate,Status,ResultImages")] RecordService rs)
        {
            ModelState.Remove("Record");
            ModelState.Remove("Service");
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                if (rs.ExecuteDate == null) rs.ExecuteDate = DateTime.Now;
                if (string.IsNullOrEmpty(rs.Status)) rs.Status = "Chờ thực hiện";

                _context.Add(rs);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo chỉ định dịch vụ CLS thành công!";
                return RedirectToAction(nameof(Index));
            }

            var recordList = _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.RecordId, Display = "BA #" + m.RecordId + " - " + m.Patient.FullName })
                .ToList();
            ViewData["RecordId"] = new SelectList(recordList, "RecordId", "Display", rs.RecordId);
            ViewData["ServiceId"] = new SelectList(_context.MedicalServices, "ServiceId", "ServiceName", rs.ServiceId);
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", rs.DoctorId);
            return View(rs);
        }

        // GET: Admin/RecordService/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var rs = await _context.RecordServices.FindAsync(id);
            if (rs == null) return NotFound();

            var recordList = _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.RecordId, Display = "BA #" + m.RecordId + " - " + m.Patient.FullName })
                .ToList();
            ViewData["RecordId"] = new SelectList(recordList, "RecordId", "Display", rs.RecordId);
            ViewData["ServiceId"] = new SelectList(_context.MedicalServices, "ServiceId", "ServiceName", rs.ServiceId);
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", rs.DoctorId);
            return View(rs);
        }

        // POST: Admin/RecordService/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RsId,RecordId,ServiceId,DoctorId,ResultValue,ExecuteDate,Status,ResultImages")] RecordService rs)
        {
            if (id != rs.RsId) return NotFound();

            ModelState.Remove("Record");
            ModelState.Remove("Service");
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRs = await _context.RecordServices.FindAsync(id);
                    if (existingRs == null) return NotFound();

                    // Update fields manually to ensure correctness
                    existingRs.RecordId = rs.RecordId;
                    existingRs.ServiceId = rs.ServiceId;
                    existingRs.DoctorId = rs.DoctorId;
                    existingRs.ResultValue = rs.ResultValue;
                    existingRs.ExecuteDate = rs.ExecuteDate;
                    existingRs.Status = rs.Status;
                    existingRs.ResultImages = rs.ResultImages;

                    _context.Update(existingRs);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Cập nhật chỉ định cho Bệnh án #{existingRs.RecordId} thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RsExists(rs.RsId)) return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            var recordList = _context.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.RecordId, Display = "BA #" + m.RecordId + " - " + m.Patient.FullName })
                .ToList();
            ViewData["RecordId"] = new SelectList(recordList, "RecordId", "Display", rs.RecordId);
            ViewData["ServiceId"] = new SelectList(_context.MedicalServices, "ServiceId", "ServiceName", rs.ServiceId);
            ViewData["DoctorId"] = new SelectList(_context.Employees.Where(e => e.JobTitle == "Bác sĩ"), "EmpId", "FullName", rs.DoctorId);
            return View(rs);
        }

        // GET: Admin/RecordService/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var rs = await _context.RecordServices
                .Include(r => r.Record).ThenInclude(rec => rec.Patient)
                .Include(r => r.Service)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(m => m.RsId == id);

            if (rs == null) return NotFound();

            return View(rs);
        }

        // POST: Admin/RecordService/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rs = await _context.RecordServices.FindAsync(id);
            if (rs != null)
            {
                _context.RecordServices.Remove(rs);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa chỉ định thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RsExists(int id)
        {
            return _context.RecordServices.Any(e => e.RsId == id);
        }
    }
}
