using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace CognomeNomeAPI.Services;

public class ImageProcessor
{
    private readonly IWebHostEnvironment _env;
    public ImageProcessor(IWebHostEnvironment env) { _env = env; }

    public string SaveUploaded(Stream stream, string ext = "png")
    {
        var id = Guid.NewGuid().ToString("N") + "." + ext;
        var dir = Path.Combine(_env.WebRootPath, "assets", "generated");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, id);
        using var img = Image.Load(stream);
        img.Save(path, new PngEncoder());
        return "/assets/generated/" + id;
    }

    public string Upscale(string filePathRelative, int scale)
    {
        var full = Path.Combine(_env.WebRootPath, filePathRelative.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var img = Image.Load(full);
        var nw = img.Width * scale;
        var nh = img.Height * scale;
        img.Mutate(x => x.Resize(nw, nh, KnownResamplers.Lanczos));
        img.Mutate(x => x.GaussianSharpen());
        var outId = Guid.NewGuid().ToString("N") + ".png";
        var dir = Path.Combine(_env.WebRootPath, "assets", "generated");
        Directory.CreateDirectory(dir);
        var outPath = Path.Combine(dir, outId);
        img.Save(outPath, new PngEncoder());
        return "/assets/generated/" + outId;
    }
}
