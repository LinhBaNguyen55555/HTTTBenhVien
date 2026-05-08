using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.ViewComponents;

[ViewComponent(Name = "MainMenu")]
public class MainMenuViewComponent : ViewComponent
{
    private readonly HtttbenhvienContext _context;

    public MainMenuViewComponent(HtttbenhvienContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var menus = await _context.WebMenus
            .Where(m => m.ParentId == null)
            .OrderBy(m => m.DisplayOrder)
            .Include(m => m.InverseParent.OrderBy(c => c.DisplayOrder))
            .ToListAsync();

        foreach (var menu in menus)
        {
            if (menu.MenuName == "Chuyên khoa" || menu.MenuUrl?.Contains("Specialty") == true)
            {
                menu.InverseParent = new List<WebMenu>();
            }
        }

        return View(menus);
    }
}
