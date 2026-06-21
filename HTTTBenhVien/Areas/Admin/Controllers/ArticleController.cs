using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ArticleController : Controller
    {
        private readonly HtttbenhvienContext _context;
        private readonly IWebHostEnvironment _environment;

        public ArticleController(HtttbenhvienContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Admin/Article
        public async Task<IActionResult> Index(string searchString, string categoryFilter, string statusFilter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CategoryFilter"] = categoryFilter;
            ViewData["StatusFilter"] = statusFilter;

            var articles = _context.Articles
                .Include(a => a.Author)
                .OrderByDescending(a => a.PublishDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                articles = articles.Where(a =>
                    a.Title.Contains(searchString) ||
                    (a.Summary != null && a.Summary.Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                articles = articles.Where(a => a.Category == categoryFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                articles = articles.Where(a => a.Status == statusFilter);
            }

            return View(await articles.ToListAsync());
        }

        // GET: Admin/Article/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(m => m.ArticleId == id);

            if (article == null) return NotFound();
            return View(article);
        }

        // GET: Admin/Article/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Employees, "EmpId", "FullName");
            return View();
        }

        // POST: Admin/Article/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ArticleId,Title,Slug,Summary,Content,Category,AuthorId,Status")] Article article, IFormFile? ThumbnailFile)
        {
            ModelState.Remove("Author");
            ModelState.Remove("ThumbnailFile");

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(article.Slug))
                    article.Slug = GenerateSlug(article.Title);

                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "img", "articles");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ThumbnailFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ThumbnailFile.CopyToAsync(fileStream);
                    }
                    article.ThumbnailUrl = "/assets/img/articles/" + uniqueFileName;
                }

                article.PublishDate = DateTime.Now;
                article.ViewCount = 0;
                if (string.IsNullOrEmpty(article.Status)) article.Status = "Draft";

                _context.Add(article);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo bài viết thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["AuthorId"] = new SelectList(_context.Employees, "EmpId", "FullName", article.AuthorId);
            return View(article);
        }

        // GET: Admin/Article/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return NotFound();

            ViewData["AuthorId"] = new SelectList(_context.Employees, "EmpId", "FullName", article.AuthorId);
            return View(article);
        }

        // POST: Admin/Article/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ArticleId,Title,Slug,ThumbnailUrl,Summary,Content,Category,AuthorId,PublishDate,Status,ViewCount")] Article article, IFormFile? ThumbnailFile)
        {
            if (id != article.ArticleId) return NotFound();

            ModelState.Remove("Author");
            ModelState.Remove("ThumbnailFile");

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(article.Slug))
                        article.Slug = GenerateSlug(article.Title);

                    if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "img", "articles");
                        Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ThumbnailFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ThumbnailFile.CopyToAsync(fileStream);
                        }
                        article.ThumbnailUrl = "/assets/img/articles/" + uniqueFileName;
                    }

                    _context.Update(article);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật bài viết thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(article.ArticleId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["AuthorId"] = new SelectList(_context.Employees, "EmpId", "FullName", article.AuthorId);
            return View(article);
        }

        // GET: Admin/Article/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(m => m.ArticleId == id);

            if (article == null) return NotFound();
            return View(article);
        }

        // POST: Admin/Article/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa bài viết thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ArticleExists(int id)
        {
            return _context.Articles.Any(e => e.ArticleId == id);
        }

        private string GenerateSlug(string title)
        {
            string slug = title.ToLower().Trim();
            slug = Regex.Replace(slug, @"[àáạảãâầấậẩẫăằắặẳẵ]", "a");
            slug = Regex.Replace(slug, @"[èéẹẻẽêềếệểễ]", "e");
            slug = Regex.Replace(slug, @"[ìíịỉĩ]", "i");
            slug = Regex.Replace(slug, @"[òóọỏõôồốộổỗơờớợởỡ]", "o");
            slug = Regex.Replace(slug, @"[ùúụủũưừứựửữ]", "u");
            slug = Regex.Replace(slug, @"[ỳýỵỷỹ]", "y");
            slug = Regex.Replace(slug, @"[đ]", "d");
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }
    }
}
