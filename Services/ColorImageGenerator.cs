using protabula_com.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace protabula_com.Services;

public class ColorImageGenerator
{
    private readonly FontFamily _fontFamily;
    private const int ImageWidth = 1200;
    private const int ImageHeight = 630;

    public ColorImageGenerator(string fontsDirectory)
    {
        var collection = new FontCollection();

        var regularPath = Path.Combine(fontsDirectory, "Inter-Regular.ttf");
        var boldPath = Path.Combine(fontsDirectory, "Inter-Bold.ttf");

        if (File.Exists(regularPath))
        {
            _fontFamily = collection.Add(regularPath);
            if (File.Exists(boldPath))
            {
                collection.Add(boldPath);
            }
        }
        else
        {
            // Fallback to system fonts
            _fontFamily = SystemFonts.Get("Arial");
        }
    }

    public void GenerateColorImage(RalColor color, string outputPath, string? culture = "en")
    {
        var bgColor = ParseHexColor(color.Hex);
        var textColor = color.NeedsDarkText ? Color.Black : Color.White;
        var textColorFaded = color.NeedsDarkText
            ? Color.FromRgba(0, 0, 0, 160)
            : Color.FromRgba(255, 255, 255, 160);

        using var image = new Image<Rgba32>(ImageWidth, ImageHeight);

        image.Mutate(ctx =>
        {
            // Fill background with the RAL color
            ctx.Fill(bgColor);

            // Create fonts - smaller sizes
            var ralLabelFont = _fontFamily.CreateFont(18, FontStyle.Regular);
            var numberFont = _fontFamily.CreateFont(64, FontStyle.Bold);
            var nameFont = _fontFamily.CreateFont(28, FontStyle.Regular);
            var hexFont = _fontFamily.CreateFont(20, FontStyle.Regular);

            // Layout: left-aligned with padding
            var leftPadding = 60f;
            var bottomPadding = 60f;

            // Calculate vertical positioning from bottom
            var hexY = ImageHeight - bottomPadding - 20;
            var nameY = hexY - 40;
            var numberY = nameY - 70;
            var ralY = numberY - 30;

            // Draw "RAL" label
            DrawLeftAlignedText(ctx, "RAL", ralLabelFont, textColorFaded, leftPadding, ralY);

            // Draw the color number
            DrawLeftAlignedText(ctx, color.Number, numberFont, textColor, leftPadding, numberY);

            // Draw the color name
            var colorName = culture == "de" && !string.IsNullOrEmpty(color.NameDe)
                ? color.NameDe
                : color.Name;

            if (!string.IsNullOrEmpty(colorName))
            {
                DrawLeftAlignedText(ctx, colorName, nameFont, textColor, leftPadding, nameY);
            }

            // Draw the HEX value
            DrawLeftAlignedText(ctx, color.Hex.ToUpperInvariant(), hexFont, textColorFaded, leftPadding, hexY);
        });

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        image.SaveAsJpeg(outputPath, new JpegEncoder { Quality = 85 });
    }

    public async Task GenerateAllColorImagesAsync(
        IEnumerable<RalColor> colors,
        string outputDirectory,
        string? culture = "en",
        IProgress<(int current, int total, string colorNumber)>? progress = null)
    {
        var colorList = colors.ToList();
        var total = colorList.Count;
        var current = 0;

        foreach (var color in colorList)
        {
            current++;
            var fileName = $"{color.Slug}.jpg";
            var outputPath = Path.Combine(outputDirectory, fileName);

            GenerateColorImage(color, outputPath, culture);

            progress?.Report((current, total, color.Number));

            // Small delay to prevent overwhelming the system
            if (current % 50 == 0)
            {
                await Task.Delay(10);
            }
        }
    }

    private static void DrawLeftAlignedText(
        IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        float x,
        float y)
    {
        var textOptions = new RichTextOptions(font)
        {
            Origin = new PointF(x, y),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        ctx.DrawText(textOptions, text, color);
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 6)
        {
            var r = Convert.ToByte(hex[..2], 16);
            var g = Convert.ToByte(hex[2..4], 16);
            var b = Convert.ToByte(hex[4..6], 16);
            return Color.FromRgb(r, g, b);
        }

        return Color.Black;
    }
}
