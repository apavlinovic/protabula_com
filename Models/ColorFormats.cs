namespace protabula_com.Models;

/// <summary>
/// Represents a color in multiple color space formats.
/// </summary>
public sealed record ColorFormats
{
    public required string Hex { get; init; }
    public required (byte R, byte G, byte B) Rgb { get; init; }
    public required (int H, int S, int L) Hsl { get; init; }
    public required (int C, int M, int Y, int K) Cmyk { get; init; }
    public required (int H, double S, double V) Hsv { get; init; }
    public required (double R, double G, double B) RgbPercent { get; init; }
    public required (double X, double Y, double Z) Xyz { get; init; }
    public required (double L, double a, double b) Lab { get; init; }
    public required (double L, double u, double v) Luv { get; init; }
    public required (double L, double a, double b) HunterLab { get; init; }
    public required (double Y, double I, double Q) Yiq { get; init; }
    public required int Decimal { get; init; }

    /// <summary>
    /// Creates ColorFormats from a hex color string.
    /// </summary>
    public static ColorFormats FromHex(string hex)
    {
        var normalizedHex = ColorWrangler.NormalizeHex(hex);
        return new ColorFormats
        {
            Hex = normalizedHex,
            Rgb = ColorWrangler.HexToRgb(normalizedHex),
            Hsl = ColorWrangler.HexToHsl(normalizedHex),
            Cmyk = ColorWrangler.HexToCmyk(normalizedHex),
            Hsv = ColorWrangler.HexToHsv(normalizedHex),
            RgbPercent = ColorWrangler.HexToRgbPercent(normalizedHex),
            Xyz = ColorWrangler.HexToXyz(normalizedHex),
            Lab = ColorWrangler.HexToLabValues(normalizedHex),
            Luv = ColorWrangler.HexToLuv(normalizedHex),
            HunterLab = ColorWrangler.HexToHunterLab(normalizedHex),
            Yiq = ColorWrangler.HexToYiq(normalizedHex),
            Decimal = ColorWrangler.HexToDecimal(normalizedHex)
        };
    }

    // Formatted string helpers for display
    public string RgbString => $"rgb({Rgb.R}, {Rgb.G}, {Rgb.B})";
    public string RgbPercentString => $"{RgbPercent.R}%, {RgbPercent.G}%, {RgbPercent.B}%";
    public string HslString => $"hsl({Hsl.H}, {Hsl.S}%, {Hsl.L}%)";
    public string HsvString => $"Hue: {Hsv.H}° Saturation: {Hsv.S}% Value: {Hsv.V}%";
    public string CmykString => $"cmyk({Cmyk.C}%, {Cmyk.M}%, {Cmyk.Y}%, {Cmyk.K}%)";
    public string XyzString => $"X: {Xyz.X} Y: {Xyz.Y} Z: {Xyz.Z}";
    public string LabString => $"L: {Lab.L} a: {Lab.a} b: {Lab.b}";
    public string LuvString => $"L: {Luv.L} u: {Luv.u} v: {Luv.v}";
    public string HunterLabString => $"L: {HunterLab.L} a: {HunterLab.a} b: {HunterLab.b}";
    public string YiqString => $"Y: {Yiq.Y} I: {Yiq.I} Q: {Yiq.Q}";
}
