using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace protabula_com.Services;

public interface IColorImageService
{
    Task<string> GetOrGenerateSceneImageAsync(string colorSlug, string colorHex, string scene);
    string GetCachedImagePath(string colorSlug, string scene);
    bool ImageExists(string colorSlug, string scene);
}

public class ColorImageService : IColorImageService
{
    private readonly IWebHostEnvironment _env;
    private readonly string _cacheFolder;
    private readonly string _scenesFolder;
    private static readonly string[] ValidScenes = ["front", "side", "terrace", "window"];

    public ColorImageService(IWebHostEnvironment env)
    {
        _env = env;
        _cacheFolder = Path.Combine(_env.WebRootPath, "images", "ral-scenes");
        _scenesFolder = Path.Combine(_env.WebRootPath, "house-scenes");

        if (!Directory.Exists(_cacheFolder))
        {
            Directory.CreateDirectory(_cacheFolder);
        }
    }

    public string GetCachedImagePath(string colorSlug, string scene)
    {
        return Path.Combine(_cacheFolder, $"{colorSlug}-{scene}.jpg");
    }

    public bool ImageExists(string colorSlug, string scene)
    {
        return File.Exists(GetCachedImagePath(colorSlug, scene));
    }

    public async Task<string> GetOrGenerateSceneImageAsync(string colorSlug, string colorHex, string scene)
    {
        if (!ValidScenes.Contains(scene))
        {
            throw new ArgumentException($"Invalid scene: {scene}");
        }

        var cachedPath = GetCachedImagePath(colorSlug, scene);

        if (File.Exists(cachedPath))
        {
            return cachedPath;
        }

        await GenerateSceneImageAsync(colorHex, scene, cachedPath);
        return cachedPath;
    }

    private async Task GenerateSceneImageAsync(string colorHex, string scene, string outputPath)
    {
        var basePath = Path.Combine(_scenesFolder, scene, "render.jpg");
        var maskPath = Path.Combine(_scenesFolder, scene, "mask.png");

        if (!File.Exists(basePath) || !File.Exists(maskPath))
        {
            throw new FileNotFoundException($"Scene files not found for {scene}");
        }

        // Parse the hex color
        var color = ParseHexColor(colorHex);

        using var baseImage = await Image.LoadAsync<Rgba32>(basePath);
        using var maskImage = await Image.LoadAsync<L8>(maskPath);

        // Ensure mask is same size as base
        if (maskImage.Width != baseImage.Width || maskImage.Height != baseImage.Height)
        {
            maskImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));
        }

        // Apply color with mask using multiply blend
        baseImage.ProcessPixelRows(maskImage, (baseAccessor, maskAccessor) =>
        {
            for (int y = 0; y < baseAccessor.Height; y++)
            {
                var baseRow = baseAccessor.GetRowSpan(y);
                var maskRow = maskAccessor.GetRowSpan(y);

                for (int x = 0; x < baseRow.Length; x++)
                {
                    var maskValue = maskRow[x].PackedValue / 255f;

                    if (maskValue > 0)
                    {
                        ref var pixel = ref baseRow[x];

                        // Multiply blend: result = base * color / 255
                        var blendedR = (byte)((pixel.R * color.R / 255f) * maskValue + pixel.R * (1 - maskValue));
                        var blendedG = (byte)((pixel.G * color.G / 255f) * maskValue + pixel.G * (1 - maskValue));
                        var blendedB = (byte)((pixel.B * color.B / 255f) * maskValue + pixel.B * (1 - maskValue));

                        pixel.R = blendedR;
                        pixel.G = blendedG;
                        pixel.B = blendedB;
                    }
                }
            }
        });

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        await baseImage.SaveAsJpegAsync(outputPath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
        {
            Quality = 85
        });
    }

    private static Rgba32 ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 3)
        {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }

        if (hex.Length != 6)
        {
            throw new ArgumentException($"Invalid hex color: {hex}");
        }

        var r = Convert.ToByte(hex[..2], 16);
        var g = Convert.ToByte(hex[2..4], 16);
        var b = Convert.ToByte(hex[4..6], 16);

        return new Rgba32(r, g, b);
    }
}
