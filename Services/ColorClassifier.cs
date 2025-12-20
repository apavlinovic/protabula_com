using protabula_com.Models;

namespace protabula_com.Services;

// ColorClassifier.cs
//
// Drop-in color classification helper.
// Requires NuGet package: Colourful
//
// Usage:
//   var result = ColorClassifier.Classify("#FF5733");
//   Console.WriteLine(result.Category);      // e.g. ColorCategory.Orange
//   Console.WriteLine(result.CategoryName);  // "Orange"
//   Console.WriteLine(result.Distance);      // DeltaE to category anchor
//
//   var categoryOnly = ColorClassifier.ClassifyCategory("#00AEEF");
//   // -> ColorCategory.Cyan

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Colourful;

/// <summary>
/// High-level color groups used by ColorClassifier.
/// </summary>
public enum ColorCategory
{
    Black,
    Grey,
    White,
    Brown,
    Red,
    Orange,
    Yellow,
    Green,
    Cyan,
    Blue,
    Purple,
    Pink,
    Unknown
}

/// <summary>
/// Result of a color classification.
/// </summary>
public sealed class ColorCategoryResult
{
    /// <summary>Original hex string (normalized to #RRGGBB).</summary>
    public string Hex { get; init; } = "#000000";

    /// <summary>Classified high-level category.</summary>
    public ColorCategory Category { get; init; }

    /// <summary>Category name as a string (e.g. "Red").</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// CIEDE2000 distance to the anchor color representing this category.
    /// Lower = closer. Typical values &lt; 5 are visually very close.
    /// </summary>
    public double Distance { get; init; }

    /// <summary>RGB components (0–255).</summary>
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
}

/// <summary>
/// Drop-in static helper to classify HEX colors into a small set of
/// perceptual categories using the Colourful library and CIEDE2000.
///
/// Designed to be dependency-light in your project: one file + one NuGet.
/// </summary>
public static class ColorClassifier
{
    // Represents a category anchor color (the "prototype" for that category).
    private sealed class ColorAnchor
    {
        public ColorCategory Category { get; }
        public string Name { get; }
        public string Hex { get; }
        public LabColor Lab { get; }

        public ColorAnchor(ColorCategory category, string name, string hex, LabColor lab)
        {
            Category = category;
            Name = name;
            Hex = hex;
            Lab = lab;
        }
    }

    private static readonly IColorDifference<LabColor> _difference = new CIEDE2000ColorDifference();

    // Anchor definitions using common, "typical" colors per group.
    // We define them by hex and convert once to Lab in the static constructor.
    private static readonly IReadOnlyList<ColorAnchor> _anchors;

    static ColorClassifier()
    {
        // Define palette as hex; we'll convert to Lab once.
        var rawAnchors = new (ColorCategory Category, string Name, string Hex)[]
        {
                (ColorCategory.Black,  "Black",  "#000000"),
                (ColorCategory.Grey,   "Grey",   "#808080"),
                (ColorCategory.White,  "White",  "#FFFFFF"),
                (ColorCategory.Brown,  "Brown",  "#8B4513"), // SaddleBrown-like
                (ColorCategory.Red,    "Red",    "#FF0000"),
                (ColorCategory.Orange, "Orange", "#FFA500"),
                (ColorCategory.Yellow, "Yellow", "#FFFF00"),
                (ColorCategory.Green,  "Green",  "#008000"),
                (ColorCategory.Cyan,   "Cyan",   "#00FFFF"),
                (ColorCategory.Blue,   "Blue",   "#0000FF"),
                (ColorCategory.Purple, "Purple", "#800080"),
                (ColorCategory.Pink,   "Pink",   "#FFC0CB"),
        };

        var anchors = new List<ColorAnchor>(rawAnchors.Length);

        foreach (var (category, name, hex) in rawAnchors)
        {
            var (r, g, b) = HexToRgbInternal(hex);
            var rgb = new RGBColor(r / 255.0, g / 255.0, b / 255.0);
            var lab = ToLab(rgb);

            anchors.Add(new ColorAnchor(category, name, hex, lab));
        }

        _anchors = anchors;
    }

    /// <summary>
    /// Classify a hex color string (e.g. "#FFAA00" or "FFAA00") into a ColorCategory.
    /// </summary>
    public static ColorCategory ClassifyCategory(string hex)
    {
        return Classify(hex).Category;
    }

    /// <summary>
    /// Classify a hex color string into a ColorCategoryResult, including
    /// distance and normalized hex.
    /// </summary>
    public static ColorCategoryResult Classify(string hex)
    {
        if (hex == null) throw new ArgumentNullException(nameof(hex));

        string normalizedHex = NormalizeHex(hex);
        var (r, g, b) = HexToRgbInternal(normalizedHex);

        var rgb = new RGBColor(r / 255.0, g / 255.0, b / 255.0);
        var lab = ToLab(rgb);

        // Find the closest anchor in Lab space using CIEDE2000.
        var best = _anchors
            .Select(anchor => new
            {
                Anchor = anchor,
                Distance = _difference.ComputeDifference(lab, anchor.Lab)
            })
            .OrderBy(x => x.Distance)
            .First();

        return new ColorCategoryResult
        {
            Hex = normalizedHex,
            Category = best.Anchor.Category,
            CategoryName = best.Anchor.Name,
            Distance = best.Distance,
            R = r,
            G = g,
            B = b
        };
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

    private static string NormalizeHex(string hex)
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

    private static LabColor ToLab(RGBColor inputRgb)
    {
        var rgbWorkingSpace = RGBWorkingSpaces.sRGB;
        var rgbToLab = new ConverterBuilder().FromRGB(rgbWorkingSpace).ToLab(Illuminants.D50).Build();

        return rgbToLab.Convert(inputRgb);
    }
}
