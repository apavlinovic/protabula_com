using protabula_com.Helpers;
using protabula_com.Models;

namespace protabula_com.Services;

/// <summary>
/// Input context for color classification.
/// </summary>
public readonly record struct ColorClassificationContext(
    string Hex,
    string? Name = null,
    RalCategory? Category = null,
    string? Number = null
);

/// <summary>
/// Service for classifying colors into root color categories.
/// </summary>
public interface IRootColorClassifier
{
    /// <summary>
    /// Detects the root color category for a given color.
    /// For RAL Classic, uses the official numbering system (first digit).
    /// For other palettes, uses name detection then HSL-based classification.
    /// </summary>
    RootColor Classify(ColorClassificationContext context);
}

public sealed class RootColorClassifier : IRootColorClassifier
{
    public RootColor Classify(ColorClassificationContext context)
    {
        // For RAL Classic, use the authoritative numbering system
        if (context.Category == RalCategory.Classic && !string.IsNullOrEmpty(context.Number))
        {
            var classicResult = ColorMath.ClassifyByRalClassicNumber(context.Number, context.Hex);
            if (classicResult != RootColor.Unknown)
            {
                return classicResult;
            }
        }

        // For other palettes, try name detection first
        var fromName = DetectFromName(context.Name);
        if (fromName != RootColor.Unknown)
        {
            return fromName;
        }

        // Fall back to fast HSL-based classification
        return ColorMath.ClassifyRootColor(context.Hex);
    }

    private static RootColor DetectFromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return RootColor.Unknown;
        }

        // Check for color keywords in the name (case-insensitive)
        // Order matters: check more specific colors first
        var colorKeywords = new (string keyword, RootColor color)[]
        {
            ("yellow", RootColor.Yellow),
            ("orange", RootColor.Orange),
            ("violet", RootColor.Violet),
            ("green", RootColor.Green),
            ("blue", RootColor.Blue),
            ("grey", RootColor.Grey),
            ("gray", RootColor.Grey),
            ("brown", RootColor.Brown),
            ("white", RootColor.White),
            ("black", RootColor.Black),
            ("pink", RootColor.Pink),
            ("rose", RootColor.Rose),
            ("beige", RootColor.Beige),
            ("red", RootColor.Red),
        };

        var lowerName = name.ToLowerInvariant();

        foreach (var (keyword, color) in colorKeywords)
        {
            if (lowerName.EndsWith(keyword))
            {
                return color;
            }
        }

        return RootColor.Unknown;
    }
}
