using System.Collections.Frozen;
using protabula_com.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace protabula_com.Services;

public interface IColorImageService
{
    Task<string> GetOrGenerateSceneImageAsync(string colorSlug, string colorHex, string scene);
    string GetCachedImagePath(string colorSlug, string scene);
    bool ImageExists(string colorSlug, string scene);
    IReadOnlyList<string> GetValidScenes();

    /// <summary>
    /// Parses a filename to extract the color slug and scene name.
    /// Returns true if successful, false if the scene is invalid.
    /// </summary>
    bool TryParseFilename(string filename, out string? slug, out string? scene);
}

public class ColorImageService : IColorImageService
{
    private readonly IWebHostEnvironment _env;
    private readonly string _cacheFolder;
    private readonly string _scenesFolder;

    private static readonly string[] ValidScenes =
        ["window", "front-door", "entrance", "balcony", "window-frame-detail"];

    // Pre-sorted by length descending for greedy suffix matching
    private static readonly string[] ScenesByLengthDesc =
        ValidScenes.OrderByDescending(s => s.Length).ToArray();

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

    public IReadOnlyList<string> GetValidScenes() => ValidScenes;

    public bool TryParseFilename(string filename, out string? slug, out string? scene)
    {
        slug = null;
        scene = null;

        // Use pre-sorted array (longest first) for greedy matching
        foreach (var s in ScenesByLengthDesc)
        {
            if (filename.EndsWith($"-{s}", StringComparison.Ordinal))
            {
                scene = s;
                slug = filename[..^(s.Length + 1)];
                return true;
            }
        }

        return false;
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

    // Wrappers for ImageSharp Rgba32 pixel format - delegate math to ColorMath
    private static (float r, float g, float b) ToLinearRgb(Rgba32 p)
        => ColorMath.ToLinearRgb(p.R, p.G, p.B);

    private static Rgba32 FromLinearRgb(float r, float g, float b, byte a)
    {
        var (R, G, B) = ColorMath.FromLinearRgb(r, g, b);
        return new Rgba32(R, G, B, a);
    }

    private static float GetLuminance(Rgba32 color)
        => ColorMath.GetLuminance(color.R, color.G, color.B);

    /// <summary>
    /// Assumed color temperature for outdoor scene lighting (warm daylight).
    /// </summary>
    private const int SceneLightTemperature = 5500;

    public async Task GenerateSceneImageAsync(string colorHex, string scene, string outputPath)
    {
        var basePath = Path.Combine(_scenesFolder, scene, "render.jpg");
        var maskPath = Path.Combine(_scenesFolder, scene, "mask.png");

        if (!File.Exists(basePath) || !File.Exists(maskPath))
            throw new FileNotFoundException($"Scene files not found for {scene}");

        var (tr, tg, tb) = ColorMath.ParseHex(colorHex);
        var targetRgb = new Rgba32(tr, tg, tb);

        // Calculate perceived luminance to determine if color is light or dark
        // Using standard luminance formula in linear space
        var targetLuminance = GetLuminance(targetRgb);

        // Use different neutral base colors for light vs dark target colors
        // Light colors need a lighter neutral to avoid blown-out highlights
        // Dark colors work well with a mid-gray neutral
        var (nr, ng, nb) = targetLuminance > 0.35f
            ? ColorMath.ParseHex("#D9D9D9") // Lighter neutral for light colors (brighter output)
            : ColorMath.ParseHex("#C0C0C0"); // Lighter neutral for dark colors (brighter output)
        var neutralBase = new Rgba32(nr, ng, nb);

        // Strength lets you dial back tinting (0..1). Start with 1, reduce if needed.
        const float strength = 1.0f;

        using var baseImage = await Image.LoadAsync<Rgba32>(basePath);
        using var maskImage = await Image.LoadAsync<L8>(maskPath);

        if (maskImage.Width != baseImage.Width || maskImage.Height != baseImage.Height)
            maskImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));

        // Precompute linear target + linear neutral + tint ratio
        var (tR, tG, tB) = ColorMath.ToLinearRgb(targetRgb.R, targetRgb.G, targetRgb.B);
        var (nR, nG, nB) = ColorMath.ToLinearRgb(neutralBase.R, neutralBase.G, neutralBase.B);

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
                    m = MathF.Pow(m, 0.9f);

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
}