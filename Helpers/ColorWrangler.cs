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

    /// <summary>
    /// Convert a hex color to HSV (Hue 0-360, Saturation 0-100, Value 0-100).
    /// </summary>
    public static (int h, double s, double v) HexToHsv(string hex)
    {
        var (r, g, b) = HexToRgb(hex);
        return RgbToHsv(r, g, b);
    }

    /// <summary>
    /// Convert RGB to HSV.
    /// </summary>
    public static (int h, double s, double v) RgbToHsv(byte r, byte g, byte b)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;

        double h = 0;
        double s = max == 0 ? 0 : delta / max;
        double v = max;

        if (delta != 0)
        {
            if (max == rNorm)
                h = ((gNorm - bNorm) / delta + (gNorm < bNorm ? 6 : 0)) * 60;
            else if (max == gNorm)
                h = ((bNorm - rNorm) / delta + 2) * 60;
            else
                h = ((rNorm - gNorm) / delta + 4) * 60;
        }

        return ((int)Math.Round(h), Math.Round(s * 100, 2), Math.Round(v * 100, 2));
    }

    /// <summary>
    /// Convert a hex color to RGB Percent (0-100% for each channel).
    /// </summary>
    public static (double r, double g, double b) HexToRgbPercent(string hex)
    {
        var (r, g, b) = HexToRgb(hex);
        return (
            Math.Round(r / 255.0 * 100, 2),
            Math.Round(g / 255.0 * 100, 2),
            Math.Round(b / 255.0 * 100, 2)
        );
    }

    /// <summary>
    /// Convert a hex color to XYZ color space.
    /// </summary>
    public static (double x, double y, double z) HexToXyz(string hex)
    {
        var (r, g, b) = HexToRgb(hex);
        var rgb = new RGBColor(r / 255.0, g / 255.0, b / 255.0);

        var converter = new ConverterBuilder()
            .FromRGB(RGBWorkingSpaces.sRGB)
            .ToXYZ(Illuminants.D50)
            .Build();

        var xyz = converter.Convert(rgb);
        return (Math.Round(xyz.X * 100, 3), Math.Round(xyz.Y * 100, 3), Math.Round(xyz.Z * 100, 3));
    }

    /// <summary>
    /// Convert a hex color to CIE Luv color space.
    /// </summary>
    public static (double l, double u, double v) HexToLuv(string hex)
    {
        var (r, g, b) = HexToRgb(hex);
        var rgb = new RGBColor(r / 255.0, g / 255.0, b / 255.0);

        var converter = new ConverterBuilder()
            .FromRGB(RGBWorkingSpaces.sRGB)
            .ToLuv(Illuminants.D50)
            .Build();

        var luv = converter.Convert(rgb);
        return (Math.Round(luv.L, 3), Math.Round(luv.u, 3), Math.Round(luv.v, 3));
    }

    /// <summary>
    /// Convert a hex color to Hunter Lab color space.
    /// </summary>
    public static (double l, double a, double b) HexToHunterLab(string hex)
    {
        var (r, g, bl) = HexToRgb(hex);
        var rgb = new RGBColor(r / 255.0, g / 255.0, bl / 255.0);

        var converter = new ConverterBuilder()
            .FromRGB(RGBWorkingSpaces.sRGB)
            .ToHunterLab(Illuminants.D50)
            .Build();

        var hunterLab = converter.Convert(rgb);
        return (Math.Round(hunterLab.L, 3), Math.Round(hunterLab.a, 3), Math.Round(hunterLab.b, 3));
    }

    /// <summary>
    /// Convert a hex color to YIQ color space (NTSC).
    /// </summary>
    public static (double y, double i, double q) HexToYiq(string hex)
    {
        var (r, g, b) = HexToRgb(hex);
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        // YIQ conversion matrix
        double y = 0.299 * rNorm + 0.587 * gNorm + 0.114 * bNorm;
        double i = 0.596 * rNorm - 0.274 * gNorm - 0.322 * bNorm;
        double q = 0.211 * rNorm - 0.523 * gNorm + 0.312 * bNorm;

        return (Math.Round(y * 255, 3), Math.Round(i * 255, 3), Math.Round(q * 255, 3));
    }

    /// <summary>
    /// Convert a hex color to decimal (integer representation).
    /// </summary>
    public static int HexToDecimal(string hex)
    {
        var normalized = NormalizeHex(hex);
        var hexValue = normalized.StartsWith("#") ? normalized[1..] : normalized;
        return int.Parse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Get formatted CIE Lab values from hex.
    /// </summary>
    public static (double l, double a, double b) HexToLabValues(string hex)
    {
        var lab = HexToLab(hex);
        return (Math.Round(lab.L, 3), Math.Round(lab.a, 3), Math.Round(lab.b, 3));
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