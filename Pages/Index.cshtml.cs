using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Paintings.Data;
using Paintings.Models;

namespace Paintings.Pages;

public class IndexModel : PageModel
{
    private readonly PaintingContext _context;

    public IndexModel(PaintingContext context)
    {
        _context = context;
    }

    public List<Painting> Paintings { get; set; } = new();

    public void OnGet()
    {
        Paintings = _context.Paintings.OrderByDescending(p => p.UploadedAt).ToList();
    }
}