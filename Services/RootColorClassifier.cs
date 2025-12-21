using Colourful;
using protabula_com.Models;

namespace protabula_com.Services;

/// <summary>
/// Service for classifying colors into root color categories.
/// </summary>
public interface IRootColorClassifier
{
    /// <summary>
    /// Detects the root color category for a given color.
    /// First attempts to detect from the English name, then falls back to LAB color distance.
    /// </summary>
    RootColor Classify(string? name, string hex);
}

public sealed class RootColorClassifier : IRootColorClassifier
{
    private static readonly IColorDifference<LabColor> ColorDifference = new CIEDE2000ColorDifference();

    private static readonly IReadOnlyList<(RootColor Color, LabColor Lab)> RootColorAnchors = BuildAnchors();

    public RootColor Classify(string? name, string hex)
    {
        // First try to detect from the English name
        var fromName = DetectFromName(name);
        if (fromName != RootColor.Unknown)
        {
            return fromName;
        }

        // Fall back to color distance classification
        return ClassifyByColorDistance(hex);
    }

    private static RootColor DetectFromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return RootColor.Unknown;
        }

        // Check for color keywords in the name (case-insensitive)
        // Order matters: check more specific colors first (e.g., "rose" before "red")
        var colorKeywords = new (string keyword, RootColor color)[]
        {
            ("yellow", RootColor.Yellow),
            ("orange", RootColor.Orange),
            ("violet", RootColor.Violet),
            ("green", RootColor.Green),
            ("blue", RootColor.Blue),
            ("grey", RootColor.Grey),
            ("gray", RootColor.Grey),  // American spelling
            ("brown", RootColor.Brown),
            ("white", RootColor.White),
            ("black", RootColor.Black),
            ("pink", RootColor.Pink),
            ("rose", RootColor.Rose),
            ("beige", RootColor.Beige),
            ("red", RootColor.Red),  // Check after rose/pink to avoid false matches
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

    private static RootColor ClassifyByColorDistance(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return RootColor.Unknown;
        }

        try
        {
            var inputLab = ColorWrangler.HexToLab(hex);

            var best = RootColorAnchors
                .Select(anchor => new
                {
                    anchor.Color,
                    Distance = ColorDifference.ComputeDifference(inputLab, anchor.Lab)
                })
                .OrderBy(x => x.Distance)
                .First();

            return best.Color;
        }
        catch
        {
            return RootColor.Unknown;
        }
    }

    private static IReadOnlyList<(RootColor Color, LabColor Lab)> BuildAnchors()
    {
        var anchors = new (RootColor color, string hex)[]
        {
            (RootColor.Yellow, "#FFFF00"),
            (RootColor.Red, "#FF0000"),
            (RootColor.Green, "#008000"),
            (RootColor.Orange, "#FFA500"),
            (RootColor.Violet, "#8B00FF"),
            (RootColor.Blue, "#0000FF"),
            (RootColor.Grey, "#808080"),
            (RootColor.Brown, "#8B4513"),
            (RootColor.White, "#FFFFFF"),
            (RootColor.Black, "#000000"),
            (RootColor.Pink, "#FFC0CB"),
            (RootColor.Rose, "#FF007F"),
            (RootColor.Beige, "#F5F5DC"),
        };

        return anchors
            .Select(a => (a.color, ColorWrangler.HexToLab(a.hex)))
            .ToList();
    }
}
