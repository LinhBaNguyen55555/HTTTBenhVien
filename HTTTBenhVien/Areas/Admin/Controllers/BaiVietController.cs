using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;
using System.Security.Claims;

namespace HTTTBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class BaiVietController : Controller
{
    private readonly HtttbenhvienContext _context;

    public BaiVietController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? category, int page = 1)
    {
        int pageSize = 20;
        var query = _context.Articles
            .Include(a => a.Author)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(a => a.Title!.ToLower().Contains(search));
        }
        if (!string.IsNullOrEmpty(category))
            query = query.Where(a => a.Category == category);

        ViewBag.Search = search;
        ViewBag.Category = category;
        var total = await query.CountAsync();
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        var articles = await query
            .OrderByDescending(a => a.PublishDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = new[] { "Tin tức", "Sự kiện", "Kiến thức y khoa", "Tuyển dụng" };
        return View(articles);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = new[] { "Tin tức", "Sự kiện", "Kiến thức y khoa", "Tuyển dụng" };
        return View(new Article { PublishDate = DateTime.Now, Status = "Published" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Article model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new[] { "Tin tức", "Sự kiện", "Kiến thức y khoa", "Tuyển dụng" };
            return View(model);
        }

        var empId = int.TryParse(User.FindFirstValue("EmpId"), out var eid) ? eid : 1;
        model.AuthorId = empId;
        model.Slug = SlugGenerator.SlugGenerator.GenerateSlug(model.Title);
        model.ViewCount = 0;

        _context.Articles.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã tạo bài viết mới.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound();
        ViewBag.Categories = new[] { "Tin tức", "Sự kiện", "Kiến thức y khoa", "Tuyển dụng" };
        return View(article);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Article model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new[] { "Tin tức", "Sự kiện", "Kiến thức y khoa", "Tuyển dụng" };
            return View(model);
        }

        _context.Articles.Update(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật bài viết thành công.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound();

        _context.Articles.Remove(article);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã xóa bài viết.";
        return RedirectToAction("Index");
    }
}
