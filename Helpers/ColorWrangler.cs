using System.Globalization;
using Colourful;

public static class ColorWrangler
{
    /// <summary>
    /// Convert a hex color to LAB color space.
    /// </summary>
    public static LabColor HexToLab(string hex)
    {
        var (r, g, b) = HexToRgb(hex);
        var rgb = new RGBColor(r / 255.0, g / 255.0, b / 255.0);

        var converter = new ConverterBuilder()
            .FromRGB(RGBWorkingSpaces.sRGB)
            .ToLab(Illuminants.D50)
            .Build();

        return converter.Convert(rgb);
    }

    /// <summary>
    /// Parse a hex color into RGB bytes (0–255).
    /// Accepts "#RRGGBB" or "RRGGBB" and throws on invalid input.
    /// </summary>
    public static (byte r, byte g, byte b) HexToRgb(string hex)
    {
        var normalizedHex = NormalizeHex(hex);
        return HexToRgbInternal(normalizedHex);
    }

    /// <summary>
    /// Convert a hex color to HSL (Hue 0-360, Saturation 0-100, Lightness 0-100).
    /// </summary>
    public static (int h, int s, int l) HexToHsl(string hex)
    {
        var (r, g, b) = HexToRgb(hex);
        return RgbToHsl(r, g, b);
    }

    /// <summary>
    /// Convert RGB to HSL.
    /// </summary>
    public static (int h, int s, int l) RgbToHsl(byte r, byte g, byte b)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;

        double h = 0;
        double s = 0;
        double l = (max + min) / 2;

        if (delta != 0)
        {
            s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);

            if (max == rNorm)
                h = ((gNorm - bNorm) / delta + (gNorm < bNorm ? 6 : 0)) * 60;
            else if (max == gNorm)
                h = ((bNorm - rNorm) / delta + 2) * 60;
            else
                h = ((rNorm - gNorm) / delta + 4) * 60;
        }

        return ((int)Math.Round(h), (int)Math.Round(s * 100), (int)Math.Round(l * 100));
    }

    /// <summary>
    /// Convert a hex color to CMYK (Cyan 0-100, Magenta 0-100, Yellow 0-100, Key 0-100).
    /// </summary>
    public static (int c, int m, int y, int k) HexToCmyk(string hex)
    {
        var (r, g, b) = HexToRgb(hex);
        return RgbToCmyk(r, g, b);
    }

    /// <summary>
    /// Convert RGB to CMYK.
    /// </summary>
    public static (int c, int m, int y, int k) RgbToCmyk(byte r, byte g, byte b)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        double k = 1 - Math.Max(rNorm, Math.Max(gNorm, bNorm));

        if (k >= 1)
            return (0, 0, 0, 100);

        double c = (1 - rNorm - k) / (1 - k);
        double m = (1 - gNorm - k) / (1 - k);
        double y = (1 - bNorm - k) / (1 - k);

        return (
            (int)Math.Round(c * 100),
            (int)Math.Round(m * 100),
            (int)Math.Round(y * 100),
            (int)Math.Round(k * 100)
        );
    }

    // ---------- Internal helpers ----------

    public static string NormalizeHex(string hex)
    {
        hex = hex.Trim();

        if (hex.StartsWith("#"))
            hex = hex[1..];

        // Support 3-digit shorthand (#RGB) by expanding to #RRGGBB
        if (hex.Length == 3)
        {
            hex = new string(new[]
            {
                    hex[0], hex[0],
                    hex[1], hex[1],
                    hex[2], hex[2]
                });
        }

        if (hex.Length != 6)
            throw new ArgumentException("Hex string must be 3 or 6 hex characters (e.g. #ABC or #AABBCC).", nameof(hex));

        // Validate that all chars are valid hex digits
        for (int i = 0; i < hex.Length; i++)
        {
            if (!Uri.IsHexDigit(hex[i]))
                throw new ArgumentException("Hex string contains invalid characters.", nameof(hex));
        }

        return "#" + hex.ToUpperInvariant();
    }

    private static (byte r, byte g, byte b) HexToRgbInternal(string normalizedHex)
    {
        // normalizedHex is assumed "#RRGGBB"
        string hex = normalizedHex.StartsWith("#")
            ? normalizedHex[1..]
            : normalizedHex;

        byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return (r, g, b);
    }
}