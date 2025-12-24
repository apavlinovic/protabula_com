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
    private static readonly string[] ValidScenes = ["front", "side", "terrace", "window", "front-door", "living-room"];

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

    public string GetImageFileName(string colorSlug, string scene)
    {
        return $"{colorSlug}-{scene}.jpg";
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

        var targetRgb = ParseHexColor(colorHex);

        using var baseImage = await Image.LoadAsync<Rgba32>(basePath);
        using var maskImage = await Image.LoadAsync<L8>(maskPath);

        // Ensure mask is same size as base
        if (maskImage.Width != baseImage.Width || maskImage.Height != baseImage.Height)
        {
            maskImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));
        }

        // Luminosity-preserving colorization
        // Remaps render grayscale range to natural lighting on colored surface
        const float minLuma = 0.65f;  // #A6A6A6 shadow
        const float maxLuma = 0.94f;  // #F0F0F0 highlight
        const float shadowOutput = 0.65f;  // darkest output (65%)
        const float highlightOutput = 1.0f; // brightest output (100%)

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

                        float baseR = pixel.R / 255f;
                        float baseG = pixel.G / 255f;
                        float baseB = pixel.B / 255f;
                        float blendR = targetRgb.R / 255f;
                        float blendG = targetRgb.G / 255f;
                        float blendB = targetRgb.B / 255f;

                        // Calculate perceived luminosity from base grayscale
                        float luma = 0.299f * baseR + 0.587f * baseG + 0.114f * baseB;

                        // Remap from render range to output range
                        float normalized = (luma - minLuma) / (maxLuma - minLuma);
                        normalized = Math.Clamp(normalized, 0f, 1f);
                        float finalLuma = shadowOutput + normalized * (highlightOutput - shadowOutput);

                        // Apply target color modulated by luminosity
                        float newR = blendR * finalLuma;
                        float newG = blendG * finalLuma;
                        float newB = blendB * finalLuma;

                        // Boost saturation by pushing colors away from gray
                        const float saturationBoost = 1.15f;  // 15% more saturation
                        float gray = (newR + newG + newB) / 3f;
                        newR = gray + (newR - gray) * saturationBoost;
                        newG = gray + (newG - gray) * saturationBoost;
                        newB = gray + (newB - gray) * saturationBoost;

                        // Blend based on mask intensity
                        pixel.R = (byte)Math.Clamp((pixel.R * (1 - maskValue) + newR * 255 * maskValue), 0, 255);
                        pixel.G = (byte)Math.Clamp((pixel.G * (1 - maskValue) + newG * 255 * maskValue), 0, 255);
                        pixel.B = (byte)Math.Clamp((pixel.B * (1 - maskValue) + newB * 255 * maskValue), 0, 255);
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
