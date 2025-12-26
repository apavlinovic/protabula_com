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
    private static readonly string[] ValidScenes = ["front", "side", "terrace", "window", "front-door", "living-room", "entrance", "balcony"];

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
    
    private static float SrgbToLinear(float c)
{
    // c in [0..1]
    return (c <= 0.04045f) ? (c / 12.92f) : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
}

private static float LinearToSrgb(float c)
{
    // c in [0..1]
    c = Math.Clamp(c, 0f, 1f);
    return (c <= 0.0031308f) ? (12.92f * c) : (1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f);
}

private static (float r, float g, float b) ToLinearRgb(Rgba32 p)
{
    float r = SrgbToLinear(p.R / 255f);
    float g = SrgbToLinear(p.G / 255f);
    float b = SrgbToLinear(p.B / 255f);
    return (r, g, b);
}

private static Rgba32 FromLinearRgb(float r, float g, float b, byte a)
{
    byte R = (byte)Math.Clamp(LinearToSrgb(r) * 255f, 0f, 255f);
    byte G = (byte)Math.Clamp(LinearToSrgb(g) * 255f, 0f, 255f);
    byte B = (byte)Math.Clamp(LinearToSrgb(b) * 255f, 0f, 255f);
    return new Rgba32(R, G, B, a);
}

public async Task GenerateSceneImageAsync(string colorHex, string scene, string outputPath)
{
    var basePath = Path.Combine(_scenesFolder, scene, "render.jpg");
    var maskPath = Path.Combine(_scenesFolder, scene, "mask.png");

    if (!File.Exists(basePath) || !File.Exists(maskPath))
        throw new FileNotFoundException($"Scene files not found for {scene}");

    var targetRgb = ParseHexColor(colorHex);

    // Neutral “base frame” reference color (your suggestion: #A7A7A7)
    var neutralBase = ParseHexColor("#A7A7A7");

    // Strength lets you dial back tinting (0..1). Start with 1, reduce if needed.
    const float strength = 1.0f;

    using var baseImage = await Image.LoadAsync<Rgba32>(basePath);
    using var maskImage = await Image.LoadAsync<L8>(maskPath);

    if (maskImage.Width != baseImage.Width || maskImage.Height != baseImage.Height)
        maskImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));

    // Precompute linear target + linear neutral + tint ratio
    var (tR, tG, tB) = (SrgbToLinear(targetRgb.R / 255f), SrgbToLinear(targetRgb.G / 255f), SrgbToLinear(targetRgb.B / 255f));
    var (nR, nG, nB) = (SrgbToLinear(neutralBase.R / 255f), SrgbToLinear(neutralBase.G / 255f), SrgbToLinear(neutralBase.B / 255f));

    // Avoid divide-by-zero
    float ratioR = tR / MathF.Max(nR, 1e-6f);
    float ratioG = tG / MathF.Max(nG, 1e-6f);
    float ratioB = tB / MathF.Max(nB, 1e-6f);

    baseImage.ProcessPixelRows(maskImage, (baseAccessor, maskAccessor) =>
    {
        for (int y = 0; y < baseAccessor.Height; y++)
        {
            var baseRow = baseAccessor.GetRowSpan(y);
            var maskRow = maskAccessor.GetRowSpan(y);

            for (int x = 0; x < baseRow.Length; x++)
            {
                float m = maskRow[x].PackedValue / 255f;
                if (m <= 0f) continue;

                // Optional: soften mask response a touch (helps avoid harsh dark edges)
                // m = MathF.Pow(m, 0.9f);

                ref var px = ref baseRow[x];

                // Work in linear
                var (bR, bG, bB) = ToLinearRgb(px);

                // Tint by ratio (preserves lighting/highlights much better than multiply)
                float tintedR = bR * ratioR;
                float tintedG = bG * ratioG;
                float tintedB = bB * ratioB;

                // Blend by mask + strength
                float k = Math.Clamp(m * strength, 0f, 1f);
                float outR = bR + (tintedR - bR) * k;
                float outG = bG + (tintedG - bG) * k;
                float outB = bB + (tintedB - bB) * k;

                px = FromLinearRgb(outR, outG, outB, px.A);
            }
        }
    });

    var outputDir = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        Directory.CreateDirectory(outputDir);

    await baseImage.SaveAsJpegAsync(outputPath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 85 });
}


    // private async Task GenerateSceneImageAsync(string colorHex, string scene, string outputPath)
    // {
    //     var basePath = Path.Combine(_scenesFolder, scene, "render.jpg");
    //     var maskPath = Path.Combine(_scenesFolder, scene, "mask.png");
    //
    //     if (!File.Exists(basePath) || !File.Exists(maskPath))
    //     {
    //         throw new FileNotFoundException($"Scene files not found for {scene}");
    //     }
    //
    //     var targetRgb = ParseHexColor(colorHex);
    //
    //     using var baseImage = await Image.LoadAsync<Rgba32>(basePath);
    //     using var maskImage = await Image.LoadAsync<L8>(maskPath);
    //
    //     if (maskImage.Width != baseImage.Width || maskImage.Height != baseImage.Height)
    //     {
    //         maskImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));
    //     }
    //
    //     // Simple multiply blend
    //     baseImage.ProcessPixelRows(maskImage, (baseAccessor, maskAccessor) =>
    //     {
    //         for (int y = 0; y < baseAccessor.Height; y++)
    //         {
    //             var baseRow = baseAccessor.GetRowSpan(y);
    //             var maskRow = maskAccessor.GetRowSpan(y);
    //
    //             for (int x = 0; x < baseRow.Length; x++)
    //             {
    //                 var maskValue = maskRow[x].PackedValue / 255f;
    //
    //                 if (maskValue > 0)
    //                 {
    //                     ref var pixel = ref baseRow[x];
    //
    //                     float baseR = pixel.R / 255f;
    //                     float baseG = pixel.G / 255f;
    //                     float baseB = pixel.B / 255f;
    //                     float blendR = targetRgb.R / 255f;
    //                     float blendG = targetRgb.G / 255f;
    //                     float blendB = targetRgb.B / 255f;
    //
    //                     // Multiply blend
    //                     float newR = baseR * blendR;
    //                     float newG = baseG * blendG;
    //                     float newB = baseB * blendB;
    //
    //                     // Blend based on mask intensity
    //                     pixel.R = (byte)Math.Clamp((pixel.R * (1 - maskValue) + newR * 255 * maskValue), 0, 255);
    //                     pixel.G = (byte)Math.Clamp((pixel.G * (1 - maskValue) + newG * 255 * maskValue), 0, 255);
    //                     pixel.B = (byte)Math.Clamp((pixel.B * (1 - maskValue) + newB * 255 * maskValue), 0, 255);
    //                 }
    //             }
    //         }
    //     });
    //
    //     var outputDir = Path.GetDirectoryName(outputPath);
    //     if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
    //     {
    //         Directory.CreateDirectory(outputDir);
    //     }
    //
    //     await baseImage.SaveAsJpegAsync(outputPath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
    //     {
    //         Quality = 85
    //     });
    // }

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
