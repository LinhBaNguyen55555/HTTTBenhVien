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
            if (!string.IsNullOrEmpty(this.ImageName))
            {

                if (this.ImageName.StartsWith("http://") || this.ImageName.StartsWith("https://"))
                    return this.ImageName;
                

                return "/assets/img/imagedoctor/" + this.ImageName;
            }
            

            var name = Uri.EscapeDataString(this.FullName);
            return $"https://ui-avatars.com/api/?name={name}&size=200&background=0D8ABC&color=fff&font-size=0.4&bold=true";
        }
    }
}
