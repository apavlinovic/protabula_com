using Colourful;

namespace protabula_com.Models;

public enum RalCategory
{
    Classic,
    DesignPlus,
    Effect
}

public enum RootColor
{
    Unknown,
    Yellow,
    Red,
    Green,
    Orange,
    Violet,
    Blue,
    Grey,
    Brown,
    White,
    Black,
    Pink,
    Rose
}

public sealed class RalColor
{
    public RalColor(
        RalCategory category,
        IReadOnlyList<string> tags,
        string hex,
        decimal brightness,
        string number,
        string name,
        string nameDe)
    {
        Category = category;
        Tags = tags;
        Hex = hex;
        Brightness = brightness;
        Number = number;
        Name = name;
        NameDe = nameDe;
        RootColor = DetectRootColor(name, hex);
    }

    public RalCategory Category { get; }
    public RootColor RootColor { get; }

    public bool NeedsDarkText
    {
        get
        {
            return Tags.Any(tag => tag.Equals("dark", StringComparison.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyList<string> Tags { get; }
    public string Hex { get; }
    public decimal Brightness { get; }
    public string Number { get; }
    public string Name { get; }
    public string NameDe { get; }

    /// <summary>
    /// URL-friendly version of the color number (spaces replaced with underscores).
    /// </summary>
    public string Slug => Number.Replace(' ', '_');

    public static RalColor Empty { get; } = new RalColor(
        RalCategory.Classic,
        Array.Empty<string>(),
        "#000000",
        0m,
        string.Empty,
        string.Empty,
        string.Empty);

    private static RootColor DetectRootColor(string name, string hex)
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

    private static RootColor DetectFromName(string name)
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
            ("red", RootColor.Red),  // Check after rose/pink to avoid false matches
        };

        var lowerName = name.ToLowerInvariant();

        foreach (var (keyword, color) in colorKeywords)
        {
            if (lowerName.Contains(keyword))
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

    private static readonly IColorDifference<LabColor> ColorDifference = new CIEDE2000ColorDifference();

    private static readonly IReadOnlyList<(RootColor Color, LabColor Lab)> RootColorAnchors = BuildAnchors();

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
        };

        return anchors
            .Select(a => (a.color, ColorWrangler.HexToLab(a.hex)))
            .ToList();
    }
}
