using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paintings.Data;
using Paintings.Models;
using Paintings.Services;

namespace Paintings.Controllers;

[ApiController]
[Route("api/paintings")]
public sealed class PaintingsController : ControllerBase
{
    private const long MaxRequestSize = 20 * 1024 * 1024 + 64 * 1024;
    private readonly PaintingContext _context;
    private readonly SupabaseStorageService _storage;
    private readonly IConfiguration _configuration;

    public PaintingsController(
        PaintingContext context,
        SupabaseStorageService storage,
        IConfiguration configuration)
    {
        _context = context;
        _storage = storage;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Painting>>> GetPaintings(CancellationToken cancellationToken)
    {
        var paintings = await _context.Paintings
            .AsNoTracking()
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync(cancellationToken);

        return Ok(paintings);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Painting>> GetPainting(long id, CancellationToken cancellationToken)
    {
        var painting = await _context.Paintings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return painting is null ? NotFound() : Ok(painting);
    }

    [HttpPost]
    [RequestSizeLimit(MaxRequestSize)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Painting>> UploadPainting(
        [FromForm] CreatePaintingRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidApiKey())
        {
            return ApiKeyConfigurationMissing()
                ? Problem("The painting API is not configured with PAINTINGS_API_KEY.", statusCode: 503)
                : Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Medium) ||
            request.Width <= 0 || request.Height <= 0 || request.ImageFile is null)
        {
            return BadRequest(new { error = "Title, medium, positive dimensions, and imageFile are required." });
        }

        StoredPaintingImage? storedImage = null;
        try
        {
            storedImage = await _storage.UploadAsync(request.ImageFile, cancellationToken);
            var painting = new Painting
            {
                Title = request.Title.Trim(),
                Medium = request.Medium.Trim(),
                Width = request.Width,
                Height = request.Height,
                ImagePath = storedImage.PublicUrl
            };

            _context.Paintings.Add(painting);
            await _context.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetPainting), new { id = painting.Id }, painting);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException)
        {
            if (storedImage is not null)
            {
                await TryDeleteUploadedImageAsync(storedImage.Path, cancellationToken);
            }

            return Problem("Could not store the painting image.", statusCode: 502);
        }
        catch
        {
            if (storedImage is not null)
            {
                await TryDeleteUploadedImageAsync(storedImage.Path, cancellationToken);
            }

            throw;
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeletePainting(long id, CancellationToken cancellationToken)
    {
        if (!HasValidApiKey())
        {
            return ApiKeyConfigurationMissing()
                ? Problem("The painting API is not configured with PAINTINGS_API_KEY.", statusCode: 503)
                : Unauthorized();
        }

        var painting = await _context.Paintings.FindAsync([id], cancellationToken);
        if (painting is null)
        {
            return NotFound();
        }

        try
        {
            await _storage.DeleteAsync(painting.ImagePath, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Problem("Could not delete the painting image from storage.", statusCode: 502);
        }

        _context.Paintings.Remove(painting);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool ApiKeyConfigurationMissing() =>
        string.IsNullOrWhiteSpace(_configuration["PAINTINGS_API_KEY"]);

    private bool HasValidApiKey()
    {
        var expectedKey = _configuration["PAINTINGS_API_KEY"];
        var providedKey = Request.Headers["X-Paintings-Api-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(expectedKey) || string.IsNullOrEmpty(providedKey))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedKey),
            Encoding.UTF8.GetBytes(providedKey));
    }

    private async Task TryDeleteUploadedImageAsync(string objectPath, CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteAsync(_storage.BuildPublicUrl(objectPath), cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Keep the original request failure as the API response.
        }
    }
}

public sealed class CreatePaintingRequest
{
    public string Title { get; set; } = string.Empty;
    public string Medium { get; set; } = string.Empty;
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public IFormFile? ImageFile { get; set; }
}
