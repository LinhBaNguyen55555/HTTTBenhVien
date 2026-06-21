using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Controllers;

public class NewsController : Controller
{
    private readonly HtttbenhvienContext _context;

    public NewsController(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int page = 1, string? category = null, string? search = null)
    {
        int pageSize = 10;

        var query = _context.Articles
            .Include(a => a.Author)
            .Where(a => a.Status == "Published");

        if (!string.IsNullOrEmpty(category) && category != "all")
        {
            query = query.Where(a => a.Category == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.Title.Contains(search) ||
                a.Summary.Contains(search) ||
                a.Content.Contains(search));
        }

        var total = await query.CountAsync();
        var articles = await query
            .OrderByDescending(a => a.PublishDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.ActiveCategory = category ?? "all";
        ViewBag.Search = search;

        return View(articles);
    }

    [HttpGet]
    [Route("tin-tuc/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var article = await _context.Articles
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Slug == slug);

        if (article == null) return NotFound();


        article.ViewCount = (article.ViewCount ?? 0) + 1;
        await _context.SaveChangesAsync();

        var related = await _context.Articles
            .Where(a => a.ArticleId != article.ArticleId &&
                        a.Status == "Published" &&
                        (a.Category == article.Category || a.AuthorId == article.AuthorId))
            .OrderByDescending(a => a.PublishDate)
            .Take(4)
            .ToListAsync();

        ViewBag.Related = related;
        return View(article);
    }
}
