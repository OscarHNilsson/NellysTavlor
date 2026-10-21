using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Paintings.Data;
using Paintings.Models;
using Paintings.Services;

namespace Paintings.Pages;

[Authorize]
public class AdminModel : PageModel
{
    private readonly PaintingContext _context;
    private readonly SupabaseStorageService _storage;

    public AdminModel(PaintingContext context, SupabaseStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    [BindProperty]
    [Required]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Medium { get; set; } = string.Empty;

    [BindProperty]
    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Width { get; set; }

    [BindProperty]
    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Height { get; set; }

    [BindProperty]
    public IFormFile? ImageFile { get; set; }

    public List<Painting> ExistingPaintings { get; set; } = new();

    public void OnGet()
    {
        ExistingPaintings = _context.Paintings.OrderByDescending(p => p.UploadedAt).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ExistingPaintings = _context.Paintings.OrderByDescending(p => p.UploadedAt).ToList();
            return Page();
        }

        if (ImageFile == null || ImageFile.Length == 0)
        {
            ModelState.AddModelError("", "Please choose an image file.");
            ExistingPaintings = _context.Paintings.OrderByDescending(p => p.UploadedAt).ToList();
            return Page();
        }

        StoredPaintingImage storedImage;
        try
        {
            storedImage = await _storage.UploadAsync(ImageFile);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(ImageFile), ex.Message);
            ExistingPaintings = _context.Paintings.OrderByDescending(p => p.UploadedAt).ToList();
            return Page();
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "The image could not be uploaded to Supabase Storage.");
            ExistingPaintings = _context.Paintings.OrderByDescending(p => p.UploadedAt).ToList();
            return Page();
        }

        var painting = new Painting
        {
            Title = Title,
            Medium = Medium,
            Width = Width,
            Height = Height,
            ImagePath = storedImage.PublicUrl
        };

        try
        {
            _context.Paintings.Add(painting);
            await _context.SaveChangesAsync();
        }
        catch
        {
            try
            {
                await _storage.DeleteAsync(storedImage.PublicUrl);
            }
            catch (HttpRequestException)
            {
                // Preserve the database error; an orphaned object can be removed separately.
            }

            throw;
        }

        TempData["SuccessMessage"] = $"'{Title}' added successfully!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        var painting = await _context.Paintings.FindAsync(id);
        if (painting != null)
        {
            try
            {
                await _storage.DeleteAsync(painting.ImagePath);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "The image could not be deleted from Supabase Storage.";
                return RedirectToPage();
            }

            _context.Paintings.Remove(painting);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{painting.Title}' was deleted.";
        }

        return RedirectToPage();
    }
}
