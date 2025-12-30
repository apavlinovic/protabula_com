using System.Globalization;
using Colourful;

namespace protabula_com.Helpers;

/// <summary>
/// Consolidated color math utilities for conversions, calculations, and comparisons.
/// </summary>
public static class ColorMath
{
    #region Hex Parsing and Normalization

    /// <summary>
    /// Parses a hex color string into RGB byte values.
    /// Accepts "#RGB", "RGB", "#RRGGBB", or "RRGGBB" formats.
    /// </summary>
    public static (byte R, byte G, byte B) ParseHex(string hex)
    {
        hex = hex.Trim().TrimStart('#');

        // Expand 3-digit shorthand (#RGB -> #RRGGBB)
        if (hex.Length == 3)
        {
            hex = new string([hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]]);
        }

        if (hex.Length != 6)
        {
            throw new ArgumentException($"Invalid hex color: {hex}. Expected 3 or 6 hex characters.");
        }

        var r = Convert.ToByte(hex[..2], 16);
        var g = Convert.ToByte(hex[2..4], 16);
        var b = Convert.ToByte(hex[4..6], 16);

        return (r, g, b);
    }

    /// <summary>
    /// Parses a hex color string into normalized float RGB values (0-1 range).
    /// </summary>
    public static (float R, float G, float B) ParseHexToFloat(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return (r / 255f, g / 255f, b / 255f);
    }

    /// <summary>
    /// Converts RGB bytes to a hex string (with # prefix).
    /// </summary>
    public static string ToHex(byte r, byte g, byte b)
    {
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Normalizes a hex color string to uppercase with # prefix.
    /// </summary>
    public static string NormalizeHex(string hex)
    {
        hex = hex.Trim();

        if (hex.StartsWith('#'))
            hex = hex[1..];

        // Support 3-digit shorthand (#RGB) by expanding to #RRGGBB
        if (hex.Length == 3)
        {
            hex = new string([hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]]);
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

    #endregion

    #region sRGB / Linear RGB Conversions

    /// <summary>
    /// Converts a single sRGB channel value (0-1) to linear RGB.
    /// Uses the standard sRGB gamma transfer function.
    /// </summary>
    public static float SrgbToLinear(float c)
    {
        return c <= 0.04045f
            ? c / 12.92f
            : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    /// <summary>
    /// Converts a single linear RGB channel value (0-1) to sRGB.
    /// Inverse of SrgbToLinear.
    /// </summary>
    public static float LinearToSrgb(float c)
    {
        c = Math.Clamp(c, 0f, 1f);
        return c <= 0.0031308f
            ? 12.92f * c
            : 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;
    }

    /// <summary>
    /// Converts sRGB byte values to linear RGB float values (0-1 range).
    /// </summary>
    public static (float R, float G, float B) ToLinearRgb(byte r, byte g, byte b)
    {
        return (
            SrgbToLinear(r / 255f),
            SrgbToLinear(g / 255f),
            SrgbToLinear(b / 255f)
        );
    }

    /// <summary>
    /// Converts linear RGB float values (0-1) back to sRGB byte values.
    /// </summary>
    public static (byte R, byte G, byte B) FromLinearRgb(float r, float g, float b)
    {
        return (
            (byte)Math.Clamp(LinearToSrgb(r) * 255f, 0f, 255f),
            (byte)Math.Clamp(LinearToSrgb(g) * 255f, 0f, 255f),
            (byte)Math.Clamp(LinearToSrgb(b) * 255f, 0f, 255f)
        );
    }

    #endregion

    #region Basic Color Space Conversions

    /// <summary>
    /// Parse a hex color into RGB bytes (0–255).
    /// </summary>
    public static (byte R, byte G, byte B) HexToRgb(string hex)
    {
        return ParseHex(hex);
    }

    /// <summary>
    /// Convert a hex color to RGB Percent (0-100% for each channel).
    /// </summary>
    public static (double R, double G, double B) HexToRgbPercent(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return (
            Math.Round(r / 255.0 * 100, 2),
            Math.Round(g / 255.0 * 100, 2),
            Math.Round(b / 255.0 * 100, 2)
        );
    }

    /// <summary>
    /// Convert a hex color to HSL (Hue 0-360, Saturation 0-100, Lightness 0-100).
    /// </summary>
    public static (int H, int S, int L) HexToHsl(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return RgbToHsl(r, g, b);
    }

    /// <summary>
    /// Convert RGB to HSL.
    /// </summary>
    public static (int H, int S, int L) RgbToHsl(byte r, byte g, byte b)
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
    /// Convert a hex color to HSV (Hue 0-360, Saturation 0-100, Value 0-100).
    /// </summary>
    public static (int H, double S, double V) HexToHsv(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return RgbToHsv(r, g, b);
    }

    /// <summary>
    /// Convert RGB to HSV.
    /// </summary>
    public static (int H, double S, double V) RgbToHsv(byte r, byte g, byte b)
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
    /// Convert a hex color to CMYK (Cyan 0-100, Magenta 0-100, Yellow 0-100, Key 0-100).
    /// </summary>
    public static (int C, int M, int Y, int K) HexToCmyk(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return RgbToCmyk(r, g, b);
    }

    /// <summary>
    /// Convert RGB to CMYK.
    /// </summary>
    public static (int C, int M, int Y, int K) RgbToCmyk(byte r, byte g, byte b)
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
    /// Convert a hex color to decimal (integer representation).
    /// </summary>
    public static int HexToDecimal(string hex)
    {
        var normalized = NormalizeHex(hex);
        var hexValue = normalized.StartsWith('#') ? normalized[1..] : normalized;
        return int.Parse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    #endregion

    #region CIE Color Spaces (using Colourful library)

    /// <summary>
    /// Convert a hex color to CIE LAB color space.
    /// </summary>
    public static LabColor HexToLab(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        var rgb = new RGBColor(r / 255.0, g / 255.0, b / 255.0);

        var converter = new ConverterBuilder()
            .FromRGB(RGBWorkingSpaces.sRGB)
            .ToLab(Illuminants.D50)
            .Build();

        return converter.Convert(rgb);
    }

    /// <summary>
    /// Get formatted CIE Lab values from hex.
    /// </summary>
    public static (double L, double a, double b) HexToLabValues(string hex)
    {
        var lab = HexToLab(hex);
        return (Math.Round(lab.L, 3), Math.Round(lab.a, 3), Math.Round(lab.b, 3));
    }

    /// <summary>
    /// Convert a hex color to XYZ color space.
    /// </summary>
    public static (double X, double Y, double Z) HexToXyz(string hex)
    {
        var (r, g, b) = ParseHex(hex);
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
    public static (double L, double u, double v) HexToLuv(string hex)
    {
        var (r, g, b) = ParseHex(hex);
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
    public static (double L, double a, double b) HexToHunterLab(string hex)
    {
        var (r, g, bl) = ParseHex(hex);
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
    public static (double Y, double I, double Q) HexToYiq(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        // YIQ conversion matrix
        double y = 0.299 * rNorm + 0.587 * gNorm + 0.114 * bNorm;
        double i = 0.596 * rNorm - 0.274 * gNorm - 0.322 * bNorm;
        double q = 0.211 * rNorm - 0.523 * gNorm + 0.312 * bNorm;

        return (Math.Round(y * 255, 3), Math.Round(i * 255, 3), Math.Round(q * 255, 3));
    }

    #endregion

    #region Luminance, LRV, and Contrast

    /// <summary>
    /// Calculates relative luminance (Y) from sRGB values using Rec. 709 coefficients.
    /// Returns a value from 0 (black) to 1 (white).
    /// This is the perceptually-weighted brightness of a color.
    /// </summary>
    public static float GetLuminance(byte r, byte g, byte b)
    {
        var (linR, linG, linB) = ToLinearRgb(r, g, b);
        // Rec. 709 luminance coefficients
        return 0.2126f * linR + 0.7152f * linG + 0.0722f * linB;
    }

    /// <summary>
    /// Calculates relative luminance from a hex color string.
    /// </summary>
    public static float GetLuminance(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return GetLuminance(r, g, b);
    }

    /// <summary>
    /// Calculate Light Reflectance Value (LRV) from hex color.
    /// LRV ranges from 0 (black) to 100 (white) and indicates how much light a color reflects.
    /// </summary>
    public static double HexToLrv(string hex) => GetLrv(hex);

    /// <summary>
    /// Calculates Light Reflectance Value (LRV) from sRGB values.
    /// LRV is essentially luminance expressed as a percentage (0-100).
    /// Used in architecture and interior design to understand how light or dark a color appears.
    ///
    /// LRV 0 = absolute black (absorbs all light)
    /// LRV 100 = perfect white (reflects all light)
    /// </summary>
    public static double GetLrv(byte r, byte g, byte b)
    {
        var luminance = GetLuminance(r, g, b);
        return Math.Round(luminance * 100, 1);
    }

    /// <summary>
    /// Calculates LRV from a hex color string.
    /// </summary>
    public static double GetLrv(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return GetLrv(r, g, b);
    }

    /// <summary>
    /// Get relative luminance (0-1) from hex color.
    /// </summary>
    public static float HexToLuminance(string hex) => GetLuminance(hex);

    /// <summary>
    /// Determines if a color needs dark text for sufficient contrast.
    /// Uses WCAG relative luminance threshold.
    /// </summary>
    public static bool NeedsDarkText(byte r, byte g, byte b)
    {
        return GetLuminance(r, g, b) > 0.179f;
    }

    /// <summary>
    /// Determines if a color needs dark text from a hex string.
    /// </summary>
    public static bool NeedsDarkText(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return NeedsDarkText(r, g, b);
    }

    /// <summary>
    /// Calculates the WCAG contrast ratio between two colors.
    /// Returns a value from 1 (no contrast) to 21 (max contrast, black vs white).
    /// WCAG AA requires 4.5:1 for normal text, 3:1 for large text.
    /// WCAG AAA requires 7:1 for normal text, 4.5:1 for large text.
    /// </summary>
    public static double GetContrastRatio(string hex1, string hex2)
    {
        var l1 = GetLuminance(hex1);
        var l2 = GetLuminance(hex2);

        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);

        return Math.Round((lighter + 0.05) / (darker + 0.05), 2);
    }

    #endregion

    #region Color Difference (Delta E)

    private static readonly IColorDifference<LabColor> Ciede2000 = new CIEDE2000ColorDifference();

    /// <summary>
    /// Calculates the CIEDE2000 color difference (Delta E) between two colors.
    /// This is the most accurate perceptual color difference formula.
    ///
    /// Interpretation:
    /// - 0-1: Imperceptible difference
    /// - 1-2: Just noticeable difference
    /// - 2-5: Small difference
    /// - 5-10: Clear difference
    /// - 10+: Very different colors
    /// </summary>
    public static double GetDeltaE(string hex1, string hex2)
    {
        var lab1 = HexToLab(hex1);
        var lab2 = HexToLab(hex2);
        return Math.Round(Ciede2000.ComputeDifference(lab1, lab2), 2);
    }

    /// <summary>
    /// Gets a human-readable interpretation of a Delta E value.
    /// </summary>
    public static string GetDeltaEInterpretation(double deltaE)
    {
        return deltaE switch
        {
            < 1 => "Imperceptible",
            < 2 => "Just noticeable",
            < 5 => "Small difference",
            < 10 => "Clear difference",
            _ => "Very different"
        };
    }

    #endregion

    #region Color Temperature

    /// <summary>
    /// Estimates the perceived color temperature of a color.
    /// Returns a Kelvin value and classification (Warm, Neutral, Cool).
    ///
    /// This is a perceptual estimate based on the color's warmth/coolness,
    /// not a true correlated color temperature (which only applies to light sources).
    ///
    /// Warm colors (reds, oranges, yellows): ~2700K-4000K
    /// Neutral colors (grays, some greens): ~4000K-5500K
    /// Cool colors (blues, cyans): ~5500K-7500K+
    /// </summary>
    public static (int Kelvin, string Classification) EstimateColorTemperature(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        var (h, s, l) = HexToHsl(hex);

        // For very desaturated colors (grays), base temperature on lightness
        if (s < 10)
        {
            // Grays are neutral, but lighter grays feel slightly cooler
            var grayKelvin = 4500 + (int)((l - 50) * 20);
            grayKelvin = Math.Clamp(grayKelvin, 4000, 5500);
            return (grayKelvin, "Neutral");
        }

        // Calculate warmth based on hue position
        // Warm hues: 0-60 (red to yellow) and 300-360 (magenta to red)
        // Cool hues: 180-270 (cyan to blue-violet)
        // Transitional: 60-180 (yellow-green to cyan) and 270-300 (violet to magenta)

        int kelvin;
        string classification;

        if (h <= 30 || h >= 330)
        {
            // Red range - very warm
            kelvin = 2700 + (int)(s * 5);
            classification = "Warm";
        }
        else if (h <= 60)
        {
            // Orange to yellow - warm
            kelvin = 3000 + (int)((h - 30) * 25);
            classification = "Warm";
        }
        else if (h <= 90)
        {
            // Yellow to yellow-green - warm to neutral transition
            kelvin = 3800 + (int)((h - 60) * 30);
            classification = h < 75 ? "Warm" : "Neutral";
        }
        else if (h <= 150)
        {
            // Green range - neutral
            kelvin = 4700 + (int)((h - 90) * 10);
            classification = "Neutral";
        }
        else if (h <= 210)
        {
            // Cyan range - neutral to cool transition
            kelvin = 5300 + (int)((h - 150) * 20);
            classification = h < 180 ? "Neutral" : "Cool";
        }
        else if (h <= 270)
        {
            // Blue range - cool
            kelvin = 6500 + (int)((h - 210) * 15);
            classification = "Cool";
        }
        else if (h <= 300)
        {
            // Violet range - cool
            kelvin = 7400 - (int)((h - 270) * 30);
            classification = "Cool";
        }
        else
        {
            // Magenta range - warm
            kelvin = 3500 - (int)((h - 300) * 25);
            classification = "Warm";
        }

        // Saturation affects perceived temperature intensity
        // Higher saturation = more pronounced warm/cool feeling
        if (s < 30)
        {
            // Low saturation pulls toward neutral
            kelvin = (int)(kelvin * 0.3 + 4750 * 0.7);
            if (classification != "Neutral" && s < 20)
            {
                classification = "Neutral";
            }
        }

        kelvin = Math.Clamp(kelvin, 2700, 7500);

        return (kelvin, classification);
    }

    #endregion

    #region Hue Color Family

    /// <summary>
    /// Gets the color family name for a given hue value (0-360).
    /// </summary>
    public static string GetHueColorFamily(int hue)
    {
        hue = ((hue % 360) + 360) % 360; // Normalize to 0-359

        return hue switch
        {
            < 15 => "Red",
            < 45 => "Orange",
            < 75 => "Yellow",
            < 150 => "Green",
            < 195 => "Cyan",
            < 255 => "Blue",
            < 285 => "Purple",
            < 330 => "Magenta",
            _ => "Red"
        };
    }

    #endregion
}
