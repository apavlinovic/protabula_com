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
            Cmyk = ColorWrangler.HexToCmyk(normalizedHex)
        };
    }

    // Formatted string helpers for display
    public string RgbString => $"rgb({Rgb.R}, {Rgb.G}, {Rgb.B})";
    public string HslString => $"hsl({Hsl.H}, {Hsl.S}%, {Hsl.L}%)";
    public string CmykString => $"cmyk({Cmyk.C}%, {Cmyk.M}%, {Cmyk.Y}%, {Cmyk.K}%)";
}
