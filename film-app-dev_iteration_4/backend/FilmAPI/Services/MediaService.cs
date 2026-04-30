using FilmAPI.DTO;

namespace FilmAPI.Services;

public class MediaService : IMediaService
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private readonly IWebHostEnvironment _environment;

    public MediaService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<MediaUploadResultDTO> UploadCoverAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("Nessun file caricato");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File troppo grande. Dimensione massima: {MaxFileSizeBytes / (1024 * 1024)} MB");
        }

        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedMimeTypes.Contains(contentType))
        {
            throw new ArgumentException($"Tipo di file non supportato: {contentType}. Tipi consentiti: jpeg, png, webp");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Estensione non supportata: {extension}");
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var relativePath = Path.Combine("media", "covers", fileName);
        
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }
        
        var absolutePath = Path.Combine(webRootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var stream = new FileStream(absolutePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return new MediaUploadResultDTO
        {
            Path = $"/{relativePath.Replace('\\', '/')}",
            FileName = fileName,
            ContentType = contentType,
            Size = file.Length
        };
    }
}
