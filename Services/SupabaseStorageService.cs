using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace Paintings.Services;

public sealed record StoredPaintingImage(string Path, string PublicUrl);

public sealed class SupabaseStorageService
{
    private const long MaxImageSize = 20 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedImageTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif"
        };

    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _secretKey;
    private readonly string _bucket;

    public SupabaseStorageService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseUrl = configuration["SUPABASE_URL"]?.TrimEnd('/') ?? string.Empty;
        _secretKey = configuration["SUPABASE_SECRET_KEY"] ?? string.Empty;
        _bucket = configuration["SUPABASE_STORAGE_BUCKET"] ?? "paintings";

        if (!Uri.TryCreate(_supabaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("SUPABASE_URL must be an absolute URL.");
        }

        if (string.IsNullOrWhiteSpace(_secretKey))
        {
            throw new InvalidOperationException("SUPABASE_SECRET_KEY is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_bucket))
        {
            throw new InvalidOperationException("SUPABASE_STORAGE_BUCKET cannot be empty.");
        }
    }

    public async Task<StoredPaintingImage> UploadAsync(IFormFile image, CancellationToken cancellationToken = default)
    {
        if (image.Length is <= 0 or > MaxImageSize)
        {
            throw new ArgumentException("Image must be between 1 byte and 20 MB.", nameof(image));
        }

        if (!AllowedImageTypes.TryGetValue(image.ContentType, out var extension))
        {
            throw new ArgumentException("Only JPEG, PNG, WebP, and GIF images are supported.", nameof(image));
        }

        var objectPath = $"{Guid.NewGuid():N}{extension}";
        var uploadUri = BuildObjectUri(objectPath);

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUri);
        request.Headers.TryAddWithoutValidation("apikey", _secretKey);
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        request.Content = new StreamContent(image.OpenReadStream());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Supabase Storage upload failed ({(int)response.StatusCode}): {details}");
        }

        return new StoredPaintingImage(objectPath, BuildPublicUrl(objectPath));
    }

    public async Task DeleteAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (!TryGetObjectPath(imagePath, out var objectPath))
        {
            // Older rows may still point to files in wwwroot; there is no remote object to delete.
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete,
            new Uri($"{_supabaseUrl}/storage/v1/object/{EscapeSegment(_bucket)}"));
        request.Headers.TryAddWithoutValidation("apikey", _secretKey);
        request.Content = JsonContent.Create(new { prefixes = new[] { objectPath } });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Supabase Storage delete failed ({(int)response.StatusCode}): {details}");
        }
    }

    public string BuildPublicUrl(string objectPath) =>
        $"{_supabaseUrl}/storage/v1/object/public/{EscapeSegment(_bucket)}/{EscapeObjectPath(objectPath)}";

    private Uri BuildObjectUri(string objectPath) =>
        new($"{_supabaseUrl}/storage/v1/object/{EscapeSegment(_bucket)}/{EscapeObjectPath(objectPath)}");

    private bool TryGetObjectPath(string imagePath, out string objectPath)
    {
        objectPath = string.Empty;
        if (!Uri.TryCreate(imagePath, UriKind.Absolute, out var imageUri) ||
            !Uri.TryCreate(_supabaseUrl, UriKind.Absolute, out var baseUri) ||
            !string.Equals(imageUri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var prefix = $"/storage/v1/object/public/{_bucket}/";
        if (!imageUri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        objectPath = Uri.UnescapeDataString(imageUri.AbsolutePath[prefix.Length..]);
        return objectPath.Length > 0;
    }

    private static string EscapeObjectPath(string path) =>
        string.Join('/', path.Split('/').Select(EscapeSegment));

    private static string EscapeSegment(string segment) => Uri.EscapeDataString(segment);
}
