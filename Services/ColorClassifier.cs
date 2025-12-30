using Colourful;
using protabula_com.Helpers;
using protabula_com.Models;

namespace protabula_com.Services;

/// <summary>
/// Result of finding similar colors, grouped by RAL category.
/// </summary>
public sealed class SimilarColorsResult
{
    public IReadOnlyList<SimilarColor> Classic { get; init; } = Array.Empty<SimilarColor>();
    public IReadOnlyList<SimilarColor> DesignPlus { get; init; } = Array.Empty<SimilarColor>();
    public IReadOnlyList<SimilarColor> Effect { get; init; } = Array.Empty<SimilarColor>();
}

/// <summary>
/// A color with its perceptual distance to the reference color.
/// </summary>
public sealed class SimilarColor
{
    public required RalColor Color { get; init; }

    /// <summary>
    /// CIEDE2000 distance to the reference color.
    /// Lower = more similar. Values under 5 are very close perceptually.
    /// </summary>
    public double Distance { get; init; }
}

public interface ISimilarColorFinder
{
    /// <summary>
    /// Find similar colors to the given reference color from all RAL palettes.
    /// </summary>
    /// <param name="referenceColor">The color to find similar colors for</param>
    /// <param name="allColors">All available RAL colors to search</param>
    /// <param name="maxPerCategory">Maximum number of similar colors per category</param>
    /// <returns>Similar colors grouped by category</returns>
    SimilarColorsResult FindSimilar(RalColor referenceColor, IReadOnlyList<RalColor> allColors, int maxPerCategory = 5);
}

public sealed class SimilarColorFinder : ISimilarColorFinder
{
    private static readonly IColorDifference<LabColor> Difference = new CIEDE2000ColorDifference();

    public SimilarColorsResult FindSimilar(RalColor referenceColor, IReadOnlyList<RalColor> allColors, int maxPerCategory = 5)
    {
        var referenceLab = ColorMath.HexToLab(referenceColor.Hex);

        var colorDistances = allColors
            .Where(c => c.Number != referenceColor.Number) // Exclude the reference color itself
            .Select(c => new SimilarColor
            {
                Color = c,
                Distance = Difference.ComputeDifference(referenceLab, ColorMath.HexToLab(c.Hex))
            })
            .ToList();

        return new SimilarColorsResult
        {
            Classic = colorDistances
                .Where(c => c.Color.Category == RalCategory.Classic)
                .OrderBy(c => c.Distance)
                .Take(maxPerCategory)
                .ToList(),
            DesignPlus = colorDistances
                .Where(c => c.Color.Category == RalCategory.DesignPlus)
                .OrderBy(c => c.Distance)
                .Take(maxPerCategory)
                .ToList(),
            Effect = colorDistances
                .Where(c => c.Color.Category == RalCategory.Effect)
                .OrderBy(c => c.Distance)
                .Take(maxPerCategory)
                .ToList()
        };
    }
}
