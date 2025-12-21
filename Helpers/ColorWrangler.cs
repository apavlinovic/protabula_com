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