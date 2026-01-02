using System.Globalization;
using Colourful;
using protabula_com.Models;

namespace protabula_com.Helpers;

/// <summary>
/// Consolidated color math utilities for conversions, calculations, and comparisons.
/// </summary>
public static class ColorMath
{
    #region Cached Converters

    // Pre-built converters to avoid repeated allocations
    private static readonly IColorConverter<RGBColor, LabColor> RgbToLabConverter =
        new ConverterBuilder().FromRGB(RGBWorkingSpaces.sRGB).ToLab(Illuminants.D50).Build();

    private static readonly IColorConverter<RGBColor, XYZColor> RgbToXyzConverter =
        new ConverterBuilder().FromRGB(RGBWorkingSpaces.sRGB).ToXYZ(Illuminants.D50).Build();

    private static readonly IColorConverter<RGBColor, LuvColor> RgbToLuvConverter =
        new ConverterBuilder().FromRGB(RGBWorkingSpaces.sRGB).ToLuv(Illuminants.D50).Build();

    private static readonly IColorConverter<RGBColor, HunterLabColor> RgbToHunterLabConverter =
        new ConverterBuilder().FromRGB(RGBWorkingSpaces.sRGB).ToHunterLab(Illuminants.D50).Build();

    #endregion

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
        return RgbToLabConverter.Convert(rgb);
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
        var xyz = RgbToXyzConverter.Convert(rgb);
        return (Math.Round(xyz.X * 100, 3), Math.Round(xyz.Y * 100, 3), Math.Round(xyz.Z * 100, 3));
    }

    /// <summary>
    /// Convert a hex color to CIE Luv color space.
    /// </summary>
    public static (double L, double u, double v) HexToLuv(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        var rgb = new RGBColor(r / 255.0, g / 255.0, b / 255.0);
        var luv = RgbToLuvConverter.Convert(rgb);
        return (Math.Round(luv.L, 3), Math.Round(luv.u, 3), Math.Round(luv.v, 3));
    }

    /// <summary>
    /// Convert a hex color to Hunter Lab color space.
    /// </summary>
    public static (double L, double a, double b) HexToHunterLab(string hex)
    {
        var (r, g, bl) = ParseHex(hex);
        var rgb = new RGBColor(r / 255.0, g / 255.0, bl / 255.0);
        var hunterLab = RgbToHunterLabConverter.Convert(rgb);
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
    /// Uses LAB b* channel (yellow-blue axis) for perceptually accurate warmth detection.
    /// Positive b* = yellow/warm, Negative b* = blue/cool.
    ///
    /// Also considers:
    /// - LAB a* (red-green) for additional warmth from reds
    /// - Chroma (saturation) - low chroma colors are more neutral
    /// - Lightness - very dark/light colors have less apparent temperature
    ///
    /// Warm colors: ~2700K-4000K
    /// Neutral colors: ~4000K-5500K
    /// Cool colors: ~5500K-7500K+
    /// </summary>
    public static (int Kelvin, string Classification) EstimateColorTemperature(string hex)
    {
        var lab = HexToLab(hex);

        // Calculate chroma (saturation in LAB space)
        double chroma = Math.Sqrt(lab.a * lab.a + lab.b * lab.b);

        // For very low chroma colors (grays), return neutral
        if (chroma < 8)
        {
            // Grays are neutral, slight variation based on lightness
            var grayKelvin = 4800 + (int)((lab.L - 50) * 10);
            grayKelvin = Math.Clamp(grayKelvin, 4200, 5400);
            return (grayKelvin, "Neutral");
        }

        // Use b* as primary warmth indicator (yellow-blue axis)
        // b* typically ranges from -100 (blue) to +100 (yellow) for saturated colors
        // Also add contribution from a* (red adds warmth)
        double warmth = lab.b + (lab.a > 0 ? lab.a * 0.3 : 0);

        // Map warmth to Kelvin
        // warmth of +60 → ~2700K (very warm)
        // warmth of 0 → ~5000K (neutral)
        // warmth of -60 → ~7500K (very cool)
        int kelvin = (int)(5000 - warmth * 40);

        // Reduce temperature extremes for low chroma colors
        if (chroma < 25)
        {
            double neutralPull = 1 - (chroma / 25.0);
            kelvin = (int)(kelvin * (1 - neutralPull * 0.6) + 5000 * neutralPull * 0.6);
        }

        // Reduce temperature extremes for very dark or very light colors
        if (lab.L < 20 || lab.L > 85)
        {
            double lightnessFactor = lab.L < 20
                ? lab.L / 20.0
                : (100 - lab.L) / 15.0;
            lightnessFactor = Math.Clamp(lightnessFactor, 0.3, 1.0);
            kelvin = (int)(kelvin * lightnessFactor + 5000 * (1 - lightnessFactor));
        }

        kelvin = Math.Clamp(kelvin, 2700, 7500);

        // Determine classification based on Kelvin
        string classification = kelvin switch
        {
            < 4200 => "Warm",
            > 5800 => "Cool",
            _ => "Neutral"
        };

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

    #region Root Color Classification

    /// <summary>
    /// Classifies a color into a root color category using HSL analysis.
    /// This is a fast, heuristic-based classification suitable for categorization.
    /// </summary>
    public static RootColor ClassifyRootColor(string hex)
    {
        var (h, s, l) = HexToHsl(hex);

        // Achromatic colors (very low saturation)
        if (s < 10)
        {
            if (l > 85) return RootColor.White;
            if (l < 15) return RootColor.Black;
            return RootColor.Grey;
        }

        // Brown: orange-red hue + moderate saturation + low lightness
        if (h >= 10 && h < 50 && s >= 15 && s < 70 && l >= 10 && l < 45)
            return RootColor.Brown;

        // Beige: yellow-orange hue + low saturation + high lightness
        if (h >= 25 && h < 65 && s >= 10 && s < 45 && l >= 55 && l < 90)
            return RootColor.Beige;

        // Grey detection for low-saturation colors that aren't beige/brown
        if (s < 15)
            return RootColor.Grey;

        // Pink: magenta-red hue with high lightness
        if ((h >= 300 || h < 15) && l > 65 && s < 60)
            return RootColor.Pink;

        // Rose: magenta-red hue with medium lightness and higher saturation
        if (h >= 300 && h < 345 && l >= 40 && l <= 65)
            return RootColor.Rose;

        // Chromatic colors by hue
        return h switch
        {
            < 15 => RootColor.Red,
            < 45 => RootColor.Orange,
            < 75 => RootColor.Yellow,
            < 150 => RootColor.Green,
            < 255 => RootColor.Blue,
            < 285 => RootColor.Violet,
            < 330 => RootColor.Violet, // Magenta range -> Violet
            _ => RootColor.Red
        };
    }

    /// <summary>
    /// Classifies a RAL Classic color based on its number prefix.
    /// RAL Classic uses a systematic numbering where the first digit indicates the color family.
    /// </summary>
    public static RootColor ClassifyByRalClassicNumber(string number, string hex)
    {
        if (string.IsNullOrEmpty(number) || number.Length < 1 || !char.IsDigit(number[0]))
            return RootColor.Unknown;

        var firstDigit = number[0];

        return firstDigit switch
        {
            '1' => RootColor.Yellow,
            '2' => RootColor.Orange,
            '3' => RootColor.Red,
            '4' => RootColor.Violet,
            '5' => RootColor.Blue,
            '6' => RootColor.Green,
            '7' => RootColor.Grey,
            '8' => RootColor.Brown,
            '9' => ClassifyWhiteOrBlack(hex),
            _ => RootColor.Unknown
        };
    }

    /// <summary>
    /// Distinguishes between white and black for RAL 9xxx colors based on lightness.
    /// </summary>
    private static RootColor ClassifyWhiteOrBlack(string hex)
    {
        var (_, _, l) = HexToHsl(hex);
        return l > 50 ? RootColor.White : RootColor.Black;
    }

    #endregion

    #region Undertone Detection

    // ============================================================================
    // RGB → Lab Conversion Note:
    // ============================================================================
    // This system assumes sRGB (D65) input and converts to CIELAB (D50) using
    // the Colourful library with Bradford chromatic adaptation.
    // See RgbToLabConverter at the top of this file.
    //
    // RAL hex values are approximate digital representations. Undertone logic
    // tolerates small variance. For critical applications, use Lab reference values.
    // ============================================================================

    /// <summary>
    /// Base threshold for chroma below which a color is considered truly achromatic/neutral.
    /// Calibrated against RAL 7005 Mouse Grey (chroma ~1.9) which is described as having "no strong undertones".
    /// Lowered to 2.0 to catch subtle undertones like RAL 7022 Umbra Grey (chroma ~3.1).
    /// </summary>
    private const double BaseAchromaticThreshold = 2.0;

    /// <summary>
    /// Base threshold boundaries for undertone strength classification at mid-lightness (L*=50).
    /// Calibrated against real RAL color descriptions:
    /// - RAL 7022 Umbra Grey (chroma 3.15) = "slight" → Subtle
    /// - RAL 1013 Oyster White (chroma 10.52) = "subtle" → Weak
    /// - RAL 6004 Blue Green (chroma 12.43) = "strong" direction → Weak
    /// - RAL 1000 Green Beige (chroma 26.46) = "distinct" → Moderate
    /// - RAL 3004 Purple Red (chroma 36.40) = "significant" → Strong
    /// </summary>
    private const double BaseSubtleThreshold = 6.0;    // Barely perceptible
    private const double BaseWeakThreshold = 15.0;     // Noticeable undertone
    private const double BaseModerateThreshold = 30.0; // Clear, distinct undertone

    /// <summary>
    /// Complete undertone analysis result for a color.
    /// </summary>
    /// <param name="Primary">Primary undertone: Warm, Cool, or Neutral</param>
    /// <param name="Secondary">Secondary undertone direction (8 compass directions, or None for achromatic)</param>
    /// <param name="Strength">Undertone strength: None, Subtle, Weak, Moderate, or Strong</param>
    /// <param name="Chroma">Raw chroma value (saturation in Lab space)</param>
    /// <param name="HueAngle">Undertone direction angle in degrees (0-360)</param>
    /// <param name="L">Lab L* (lightness) value for debugging</param>
    /// <param name="a">Lab a* value for debugging</param>
    /// <param name="b">Lab b* value for debugging</param>
    public readonly record struct UndertoneResult(
        PrimaryUndertone Primary,
        SecondaryUndertone Secondary,
        UndertoneStrength Strength,
        double Chroma,
        double HueAngle,
        double L,
        double a,
        double b
    )
    {
        /// <summary>
        /// Human-readable description combining primary, strength, and secondary.
        /// E.g., "Warm with strong yellow undertone", "Neutral with subtle green undertone", or "Neutral"
        /// </summary>
        public string Description => (Primary, Secondary, Strength) switch
        {
            // Truly achromatic - no undertone at all
            (_, SecondaryUndertone.None, _) => "Neutral",
            (_, _, UndertoneStrength.None) => "Neutral",

            // Near-neutral with detectable undertone direction (subtle bias)
            (PrimaryUndertone.Neutral, _, _) => $"Neutral with {Strength.ToString().ToLowerInvariant()} {Secondary.ToString().ToLowerInvariant()} undertone",

            // Clear warm/cool with undertone
            _ => $"{Primary} with {Strength.ToString().ToLowerInvariant()} {Secondary.ToString().ToLowerInvariant()} undertone"
        };

        /// <summary>
        /// Returns true if the color has a strong, perceptible undertone.
        /// </summary>
        public bool HasStrongUndertone => Strength == UndertoneStrength.Strong && Secondary != SecondaryUndertone.None;

        /// <summary>
        /// Returns true if the undertone is worth mentioning (Moderate or higher).
        /// </summary>
        public bool IsSignificant => Strength >= UndertoneStrength.Moderate && Secondary != SecondaryUndertone.None;
    }

    /// <summary>
    /// Analyzes the undertone of a color from its hex value.
    /// Uses Lab color space where:
    /// - a* axis: +a* = red (warm), -a* = green (cool)
    /// - b* axis: +b* = yellow (warm), -b* = blue (cool)
    /// </summary>
    public static UndertoneResult AnalyzeUndertone(string hex)
    {
        var lab = HexToLab(hex);
        return AnalyzeUndertoneFromLab(lab.L, lab.a, lab.b);
    }

    /// <summary>
    /// Analyzes the undertone from Lab values.
    /// </summary>
    public static UndertoneResult AnalyzeUndertoneFromLab(double L, double a, double b)
    {
        // Calculate chroma (saturation in Lab space)
        double chroma = Math.Sqrt(a * a + b * b);

        // Calculate hue angle in degrees (0-360)
        // atan2 returns radians in range [-pi, pi]
        double angleRad = Math.Atan2(b, a);
        double angleDeg = angleRad * (180.0 / Math.PI);
        if (angleDeg < 0) angleDeg += 360;

        // Get L*-aware thresholds - undertone perception varies with lightness
        var (achromaticThreshold, subtleThreshold, weakThreshold, moderateThreshold) =
            GetLightnessAdjustedThresholds(L);

        // Handle achromatic colors (grays, pure whites, pure blacks)
        if (chroma < achromaticThreshold)
        {
            return new UndertoneResult(
                Primary: PrimaryUndertone.Neutral,
                Secondary: SecondaryUndertone.None,
                Strength: UndertoneStrength.None,
                Chroma: Math.Round(chroma, 2),
                HueAngle: 0,
                L: Math.Round(L, 2),
                a: Math.Round(a, 3),
                b: Math.Round(b, 3)
            );
        }

        // Determine undertone strength based on L*-adjusted thresholds
        var strength = DetermineUndertoneStrength(chroma, subtleThreshold, weakThreshold, moderateThreshold);

        // Determine secondary undertone based on hue angle
        // For low-strength undertones, collapse to 4 cardinal directions for stability
        var secondary = DetermineSecondaryUndertone(angleDeg, strength);

        // Determine primary undertone using hue angle sectors
        var primary = DeterminePrimaryUndertone(angleDeg, chroma, achromaticThreshold);

        return new UndertoneResult(
            Primary: primary,
            Secondary: secondary,
            Strength: strength,
            Chroma: Math.Round(chroma, 2),
            HueAngle: Math.Round(angleDeg, 1),
            L: Math.Round(L, 2),
            a: Math.Round(a, 3),
            b: Math.Round(b, 3)
        );
    }

    /// <summary>
    /// Returns L*-adjusted thresholds for undertone detection.
    /// Dark colors (low L*) need higher thresholds because undertones are harder to perceive.
    /// Light colors (high L*) need lower thresholds because undertones are more visible.
    /// </summary>
    private static (double Achromatic, double Subtle, double Weak, double Moderate) GetLightnessAdjustedThresholds(double L)
    {
        // Scale factor based on lightness:
        // - L* = 50 (mid-gray): factor = 1.0 (base thresholds)
        // - L* = 10 (very dark): factor ≈ 1.4 (higher thresholds, undertones harder to see)
        // - L* = 90 (very light): factor ≈ 0.7 (lower thresholds, undertones more visible)
        //
        // The relationship is roughly linear in the mid-range but we clamp extremes.
        double factor;
        if (L < 20)
        {
            // Very dark colors: undertones are hard to perceive
            factor = 1.4;
        }
        else if (L > 80)
        {
            // Very light colors: undertones are very visible
            factor = 0.7;
        }
        else
        {
            // Linear interpolation between L=20 (factor=1.4) and L=80 (factor=0.7)
            factor = 1.4 - (L - 20) * (0.7 / 60);
        }

        return (
            Achromatic: BaseAchromaticThreshold * factor,
            Subtle: BaseSubtleThreshold * factor,
            Weak: BaseWeakThreshold * factor,
            Moderate: BaseModerateThreshold * factor
        );
    }

    /// <summary>
    /// Determines the undertone strength based on chroma and L*-adjusted thresholds.
    /// </summary>
    private static UndertoneStrength DetermineUndertoneStrength(
        double chroma,
        double subtleThreshold,
        double weakThreshold,
        double moderateThreshold)
    {
        return chroma switch
        {
            _ when chroma < subtleThreshold => UndertoneStrength.Subtle,
            _ when chroma < weakThreshold => UndertoneStrength.Weak,
            _ when chroma < moderateThreshold => UndertoneStrength.Moderate,
            _ => UndertoneStrength.Strong
        };
    }

    /// <summary>
    /// Threshold multiplier for "near-neutral" zone where Primary returns Neutral
    /// even though a secondary direction is detectable. This prevents over-claiming
    /// undertones on borderline grey colors.
    /// </summary>
    private const double NearNeutralMultiplier = 2.5;

    /// <summary>
    /// Determines the primary undertone (Warm/Cool/Neutral) using hue angle sectors.
    /// This is more stable than weighted a*/b* calculations, especially near neutral.
    ///
    /// Warm sector: 315° to 135° (red through yellow through olive)
    /// Cool sector: 135° to 315° (green through blue through violet)
    /// </summary>
    private static PrimaryUndertone DeterminePrimaryUndertone(double angleDeg, double chroma, double achromaticThreshold)
    {
        // Near-neutral zone: colors with chroma above achromatic threshold but still
        // too low to confidently claim warm/cool. These return Neutral for Primary
        // but may still have a detectable Secondary direction.
        //
        // This prevents over-claiming on borderline greys like RAL 7042 Traffic Grey A
        // which is "slightly cool but perceptually neutral".
        double nearNeutralThreshold = achromaticThreshold * NearNeutralMultiplier;
        if (chroma < nearNeutralThreshold)
        {
            return PrimaryUndertone.Neutral;
        }

        // Use hue angle to determine warm vs cool
        // Warm sector: 315° → 0° → 135° (reds, oranges, yellows, olive-yellows)
        // Cool sector: 135° → 180° → 315° (olives, greens, teals, blues, violets)
        //
        // Note: The boundary is at ~135° (olive) and ~315° (violet) which are
        // the transitions between warm and cool perception.
        bool isWarmHue = angleDeg is (>= 315 or < 135);

        return isWarmHue ? PrimaryUndertone.Warm : PrimaryUndertone.Cool;
    }

    /// <summary>
    /// Determines the specific undertone direction using 8 compass directions for strong undertones,
    /// or 4 cardinal directions for weak undertones (to reduce noise).
    ///
    /// Based on the hue angle in Lab a*/b* space where:
    /// - 0° = pure red (+a*)
    /// - 90° = pure yellow (+b*)
    /// - 180° = pure green (-a*)
    /// - 270° = pure blue (-b*)
    /// </summary>
    private static SecondaryUndertone DetermineSecondaryUndertone(double angleDeg, UndertoneStrength strength)
    {
        // For subtle undertones, collapse to 4 cardinal directions for stability
        // This reduces noise where low-chroma colors might flip between adjacent 8-way directions
        if (strength == UndertoneStrength.Subtle)
        {
            return angleDeg switch
            {
                // Red: 315° - 45°
                >= 315 or < 45 => SecondaryUndertone.Red,
                // Yellow: 45° - 135°
                >= 45 and < 135 => SecondaryUndertone.Yellow,
                // Green: 135° - 225°
                >= 135 and < 225 => SecondaryUndertone.Green,
                // Blue: 225° - 315°
                _ => SecondaryUndertone.Blue
            };
        }

        // For Weak and above, use full 8-direction compass
        // Each direction covers 45° (360° / 8 directions)
        return angleDeg switch
        {
            // Red: centered at 0°, covers 337.5° - 22.5°
            >= 337.5 or < 22.5 => SecondaryUndertone.Red,

            // Orange: centered at 45°, covers 22.5° - 67.5° (warm+warm: red+yellow)
            >= 22.5 and < 67.5 => SecondaryUndertone.Orange,

            // Yellow: centered at 90°, covers 67.5° - 112.5°
            >= 67.5 and < 112.5 => SecondaryUndertone.Yellow,

            // Olive: centered at 135°, covers 112.5° - 157.5° (cool+warm: green+yellow)
            >= 112.5 and < 157.5 => SecondaryUndertone.Olive,

            // Green: centered at 180°, covers 157.5° - 202.5°
            >= 157.5 and < 202.5 => SecondaryUndertone.Green,

            // Teal: centered at 225°, covers 202.5° - 247.5° (cool+cool: green+blue)
            >= 202.5 and < 247.5 => SecondaryUndertone.Teal,

            // Blue: centered at 270°, covers 247.5° - 292.5°
            >= 247.5 and < 292.5 => SecondaryUndertone.Blue,

            // Violet: centered at 315°, covers 292.5° - 337.5° (warm+cool: red+blue)
            _ => SecondaryUndertone.Violet
        };
    }

    // Pre-built converter for Lab to RGB
    private static readonly IColorConverter<LabColor, RGBColor> LabToRgbConverter =
        new ConverterBuilder().FromLab(Illuminants.D50).ToRGB(RGBWorkingSpaces.sRGB).Build();

    /// <summary>
    /// Extracts the undertone direction as a visible color.
    /// Creates a color that represents the chromatic direction (undertone) of the original color,
    /// with amplified saturation so the undertone is clearly visible.
    /// </summary>
    /// <param name="hex">The original color hex value</param>
    /// <returns>A hex color representing the undertone direction, or null for neutral colors</returns>
    public static string? ExtractUndertoneColor(string hex)
    {
        var lab = HexToLab(hex);
        return ExtractUndertoneColorFromLab(lab.L, lab.a, lab.b);
    }

    /// <summary>
    /// Extracts the undertone direction as a visible color from Lab values.
    /// Uses gamut-safe amplification that reduces chroma until the result fits in sRGB.
    /// </summary>
    public static string? ExtractUndertoneColorFromLab(double L, double a, double b)
    {
        // Calculate chroma
        double chroma = Math.Sqrt(a * a + b * b);

        // Get L*-adjusted achromatic threshold
        var (achromaticThreshold, _, _, _) = GetLightnessAdjustedThresholds(L);

        // For achromatic colors, there's no undertone to visualize
        if (chroma < achromaticThreshold)
        {
            return null;
        }

        // Use a lightness that makes the undertone clearly visible
        // Lighter colors get a slightly darker undertone swatch, darker colors get lighter
        double undertoneL = L > 60 ? 55 : 65;

        // Target chroma of 45-55 for a clearly visible but not oversaturated undertone
        double targetChroma = Math.Min(55, Math.Max(40, chroma * 1.5));

        // Calculate the unit direction vector in a*/b* space
        double dirA = a / chroma;
        double dirB = b / chroma;

        // Try to find a chroma that fits within sRGB gamut
        // Start with target chroma and reduce if out of gamut
        for (double tryChroma = targetChroma; tryChroma >= 10; tryChroma -= 5)
        {
            double amplifiedA = dirA * tryChroma;
            double amplifiedB = dirB * tryChroma;

            var undertoneLabColor = new LabColor(undertoneL, amplifiedA, amplifiedB);
            var rgbColor = LabToRgbConverter.Convert(undertoneLabColor);

            // Check if in gamut (all channels 0-1 with small tolerance)
            if (IsInSrgbGamut(rgbColor.R, rgbColor.G, rgbColor.B))
            {
                byte r = (byte)Math.Round(Math.Clamp(rgbColor.R, 0, 1) * 255);
                byte g = (byte)Math.Round(Math.Clamp(rgbColor.G, 0, 1) * 255);
                byte bl = (byte)Math.Round(Math.Clamp(rgbColor.B, 0, 1) * 255);
                return ToHex(r, g, bl);
            }
        }

        // Fallback: use a very low chroma that should always be in gamut
        double safeA = dirA * 10;
        double safeB = dirB * 10;
        var safeLabColor = new LabColor(undertoneL, safeA, safeB);
        var safeRgbColor = LabToRgbConverter.Convert(safeLabColor);

        byte safeR = (byte)Math.Round(Math.Clamp(safeRgbColor.R, 0, 1) * 255);
        byte safeG = (byte)Math.Round(Math.Clamp(safeRgbColor.G, 0, 1) * 255);
        byte safeBl = (byte)Math.Round(Math.Clamp(safeRgbColor.B, 0, 1) * 255);
        return ToHex(safeR, safeG, safeBl);
    }

    /// <summary>
    /// Checks if RGB values are within the sRGB gamut (0-1 range with small tolerance).
    /// </summary>
    private static bool IsInSrgbGamut(double r, double g, double b)
    {
        const double tolerance = 0.001;
        return r >= -tolerance && r <= 1 + tolerance &&
               g >= -tolerance && g <= 1 + tolerance &&
               b >= -tolerance && b <= 1 + tolerance;
    }

    #endregion
}
