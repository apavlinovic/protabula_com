using Colourful;
using protabula_com.Helpers;
using protabula_com.Models;

namespace protabula_com.Services;

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
    /// Find similar colors from the same category as the reference color.
    /// </summary>
    IReadOnlyList<SimilarColor> FindSimilarInCategory(
        RalColor referenceColor,
        IReadOnlyList<RalColor> allColors,
        int maxCount = 10);

    /// <summary>
    /// Find all colors with the same root color in the same category.
    /// </summary>
    IReadOnlyList<RalColor> FindSameRootColorInCategory(
        RalColor referenceColor,
        IReadOnlyList<RalColor> allColors);
}

public sealed class SimilarColorFinder : ISimilarColorFinder
{
    private static readonly IColorDifference<LabColor> Difference = new CIEDE2000ColorDifference();

    public IReadOnlyList<SimilarColor> FindSimilarInCategory(
        RalColor referenceColor,
        IReadOnlyList<RalColor> allColors,
        int maxCount = 10)
    {
        var referenceLab = ColorMath.HexToLab(referenceColor.Hex);

        return allColors
            .Where(c => c.Number != referenceColor.Number && c.Category == referenceColor.Category)
            .Select(c => new SimilarColor
            {
                Color = c,
                Distance = Difference.ComputeDifference(referenceLab, ColorMath.HexToLab(c.Hex))
            })
            .OrderBy(c => c.Distance)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<RalColor> FindSameRootColorInCategory(
        RalColor referenceColor,
        IReadOnlyList<RalColor> allColors)
    {
        return allColors
            .Where(c => c.Number != referenceColor.Number
                     && c.Category == referenceColor.Category
                     && c.RootColor == referenceColor.RootColor)
            .ToList();
    }
}
