namespace protabula_com.Helpers;

/// <summary>
/// Core color math utilities for sRGB/linear conversions, luminance, and LRV calculations.
/// Consolidates color math that was previously spread across multiple services.
/// </summary>
public static class ColorMath
{
    #region Hex Parsing

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

    #region Luminance and LRV

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

    #endregion

    #region Contrast Ratio

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
}
