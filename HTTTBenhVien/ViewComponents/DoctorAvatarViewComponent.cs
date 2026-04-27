using Microsoft.AspNetCore.Mvc;

namespace HTTTBenhVien.ViewComponents;

public class DoctorAvatarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string? imageName, string? fullName, string? cssClass = null)
    {
        var model = new DoctorAvatarViewModel
        {
            ImageName = imageName,
            FullName = fullName ?? "D",
            CssClass = cssClass ?? ""
        };
        return View(model);
    }
}

public class DoctorAvatarViewModel
{
    public string? ImageName { get; set; }
    public string FullName { get; set; } = "D";
    public string CssClass { get; set; } = "";

    public string AvatarUrl
    {
        get
        {
            const string basePath = "/assets/img/imagedoctor/";
            const string defaultImage = "default-avatar.jpg";
            return basePath + (string.IsNullOrEmpty(this.ImageName) ? defaultImage : this.ImageName);
        }
    }
}
