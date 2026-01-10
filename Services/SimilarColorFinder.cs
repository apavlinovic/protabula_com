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

/// <summary>
/// A color with mood similarity metrics to the reference color.
/// </summary>
public sealed class MoodSimilarColor
{
    public required RalColor Color { get; init; }

    /// <summary>
    /// Number of mood tags shared with the reference color.
    /// </summary>
    public int SharedTagCount { get; init; }

    /// <summary>
    /// Jaccard similarity index (shared / union). Range 0-1, higher = more similar.
    /// </summary>
    public double JaccardIndex { get; init; }

    /// <summary>
    /// The mood tags shared between both colors.
    /// </summary>
    public IReadOnlyList<string> SharedTags { get; init; } = [];
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

    /// <summary>
    /// Find colors with similar mood tags, ranked by Jaccard similarity.
    /// </summary>
    IReadOnlyList<MoodSimilarColor> FindSimilarByMood(
        RalColor referenceColor,
        IReadOnlyList<RalColor> allColors,
        int minSharedTags = 2,
        int maxCount = 8);
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

    public IReadOnlyList<MoodSimilarColor> FindSimilarByMood(
        RalColor referenceColor,
        IReadOnlyList<RalColor> allColors,
        int minSharedTags = 2,
        int maxCount = 8)
    {
        if (referenceColor.MoodTags.Count == 0)
            return [];

        var referenceTags = referenceColor.MoodTags.ToHashSet();

        return allColors
            .Where(c => c.Number != referenceColor.Number && c.MoodTags.Count > 0)
            .Select(c =>
            {
                var colorTags = c.MoodTags.ToHashSet();
                var sharedTags = referenceTags.Intersect(colorTags).ToList();
                var unionCount = referenceTags.Union(colorTags).Count();
                var jaccard = unionCount > 0 ? (double)sharedTags.Count / unionCount : 0;

                return new MoodSimilarColor
                {
                    Color = c,
                    SharedTagCount = sharedTags.Count,
                    JaccardIndex = jaccard,
                    SharedTags = sharedTags
                };
            })
            .Where(m => m.SharedTagCount >= minSharedTags)
            .OrderByDescending(m => m.JaccardIndex)
            .ThenByDescending(m => m.SharedTagCount)
            .Take(maxCount)
            .ToList();
    }
}
