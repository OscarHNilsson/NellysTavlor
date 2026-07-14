using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Paintings.Pages;

public class IndexModel : PageModel
{

    public List<string> ImageFiles { get; set; } = new();

    public void OnGet()
    {
        var imageDir = Path.Combine("wwwroot", "images");
        ImageFiles = Directory.GetFiles(imageDir)
            .Select(f => "/images/" + Path.GetFileName(f))
            .ToList();
    }
}
