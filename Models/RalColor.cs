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
    Rose,
    Beige
}

public static class RootColorExtensions
{
    private static readonly RootColor[] DisplayOrder =
    [
        RootColor.Black,
        RootColor.Grey,
        RootColor.White,
        RootColor.Beige,
        RootColor.Red,
        RootColor.Orange,
        RootColor.Yellow,
        RootColor.Green,
        RootColor.Blue,
        RootColor.Violet,
        RootColor.Pink,
        RootColor.Rose,
        RootColor.Brown
    ];

    public static int GetDisplayOrder(this RootColor color)
    {
        var index = Array.IndexOf(DisplayOrder, color);
        return index >= 0 ? index : int.MaxValue;
    }
}

/// <summary>
/// Primary undertone classification based on perceived color temperature.
/// </summary>
public enum PrimaryUndertone
{
    /// <summary>Colors with dominant yellow/red bias (+a* and/or +b* in Lab)</summary>
    Warm,

    /// <summary>Colors with dominant blue/green bias (-a* and/or -b* in Lab)</summary>
    Cool,

    /// <summary>Colors with balanced or negligible chromatic bias (low chroma)</summary>
    Neutral
}

/// <summary>
/// Secondary undertone indicating the specific chromatic direction.
/// Uses 8 directions like a compass, derived from the hue angle in Lab a*/b* space.
/// </summary>
public enum SecondaryUndertone
{
    /// <summary>No discernible undertone (achromatic or very low chroma)</summary>
    None,

    /// <summary>Pure +b* axis: golden, honey tones (67.5° - 112.5°)</summary>
    Yellow,

    /// <summary>+a* and +b* quadrant: peach, coral, brown-ish, copper (22.5° - 67.5°)</summary>
    Orange,

    /// <summary>Pure +a* axis: pink-red, rose, salmon (337.5° - 22.5°)</summary>
    Red,

    /// <summary>+a* and -b* quadrant: mauve, purple, pink-blue, berry (292.5° - 337.5°)</summary>
    Violet,

    /// <summary>Pure -b* axis: steel, icy, navy (247.5° - 292.5°)</summary>
    Blue,

    /// <summary>-a* and -b* quadrant: cyan, teal, steel-green (202.5° - 247.5°)</summary>
    Teal,

    /// <summary>Pure -a* axis: sage, mint, forest (157.5° - 202.5°)</summary>
    Green,

    /// <summary>-a* and +b* quadrant: khaki, olive, yellow-green (112.5° - 157.5°)</summary>
    Olive
}

/// <summary>
/// Undertone strength based on chroma (color saturation in Lab space).
/// Calibrated against real RAL color descriptions.
/// </summary>
public enum UndertoneStrength
{
    /// <summary>No perceptible undertone (achromatic colors)</summary>
    None,

    /// <summary>Barely perceptible undertone (e.g., RAL 7022 Umbra Grey "slight")</summary>
    Subtle,

    /// <summary>Noticeable undertone (e.g., RAL 1013 Oyster White "subtle")</summary>
    Weak,

    /// <summary>Clear, distinct undertone (e.g., RAL 1000 Green Beige "distinct")</summary>
    Moderate,

    /// <summary>Dominant undertone (e.g., RAL 3004 Purple Red "significant")</summary>
    Strong
}

/// <summary>
/// Light Reflectance Value classification for descriptive purposes.
/// </summary>
public enum LrvLevel
{
    /// <summary>LRV 0-15: Absorbs most light, creating depth and visual weight</summary>
    VeryLow,

    /// <summary>LRV 15-30: Absorbs more light than it reflects, providing a grounding effect</summary>
    Low,

    /// <summary>LRV 30-50: Reflects a balanced level of light, versatile for many applications</summary>
    Moderate,

    /// <summary>LRV 50-70: Reflects more light than it absorbs, creating an airy, open feel</summary>
    High,

    /// <summary>LRV 70-100: Reflects most light, maximizing brightness and perceived space</summary>
    VeryHigh
}

public sealed class RalColor
{
    public RalColor(
        RalCategory category,
        RootColor rootColor,
        IReadOnlyList<string> tags,
        string hex,
        decimal brightness,
        double lrv,
        string number,
        string name,
        string nameDe,
        string? descriptionEn = null,
        string? descriptionDe = null,
        IReadOnlyList<string>? usageTags = null,
        IReadOnlyList<string>? moodTags = null)
    {
        Category = category;
        RootColor = rootColor;
        Tags = tags;
        Hex = hex;
        Brightness = brightness;
        Lrv = lrv;
        Number = number;
        Name = name;
        NameDe = nameDe;
        DescriptionEn = descriptionEn;
        DescriptionDe = descriptionDe;
        UsageTags = usageTags ?? Array.Empty<string>();
        MoodTags = moodTags ?? Array.Empty<string>();
    }

    public RalCategory Category { get; }
    public RootColor RootColor { get; }

    public bool NeedsDarkText => Tags.Any(tag => tag.Equals("dark", StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> Tags { get; }
    public string Hex { get; }
    public decimal Brightness { get; }

    /// <summary>
    /// Light Reflectance Value (0-100). Pre-calculated from official RAL data.
    /// </summary>
    public double Lrv { get; }

    public string Number { get; }
    public string Name { get; }
    public string NameDe { get; }
    public string? DescriptionEn { get; }
    public string? DescriptionDe { get; }

    /// <summary>
    /// Usage context tags (e.g., ARCHITECTURE, INDUSTRIAL, INTERIOR_DESIGN).
    /// </summary>
    public IReadOnlyList<string> UsageTags { get; }

    /// <summary>
    /// Mood/feeling tags (e.g., WARM, NATURAL, ELEGANT).
    /// </summary>
    public IReadOnlyList<string> MoodTags { get; }

    /// <summary>
    /// Returns true if the color has a description available.
    /// </summary>
    public bool HasDescription => !string.IsNullOrEmpty(DescriptionEn);

    /// <summary>
    /// Gets the localized color name for the specified culture.
    /// Falls back to English if the requested language is not available.
    /// </summary>
    /// <param name="culture">Two-letter ISO language code (e.g., "en", "de")</param>
    public string GetLocalizedName(string culture) => culture switch
    {
        "de" => !string.IsNullOrEmpty(NameDe) ? NameDe : Name,
        _ => Name
    };

    /// <summary>
    /// Gets the localized description for the specified culture.
    /// Falls back to English if the requested language is not available.
    /// Returns null if no description exists.
    /// </summary>
    /// <param name="culture">Two-letter ISO language code (e.g., "en", "de")</param>
    public string? GetLocalizedDescription(string culture) => culture switch
    {
        "de" => !string.IsNullOrEmpty(DescriptionDe) ? DescriptionDe : DescriptionEn,
        _ => DescriptionEn
    };

    /// <summary>
    /// URL-friendly version of the color number (spaces replaced with underscores).
    /// </summary>
    public string Slug => ToSlug(Number);

    /// <summary>
    /// Converts a color number to a URL-friendly slug (spaces to underscores).
    /// </summary>
    public static string ToSlug(string colorNumber) => colorNumber.Replace(' ', '_');

    /// <summary>
    /// Converts a URL slug back to a color number (underscores to spaces).
    /// </summary>
    public static string FromSlug(string slug) => slug.Replace('_', ' ');

    public static RalColor Empty { get; } = new RalColor(
        RalCategory.Classic,
        RootColor.Unknown,
        Array.Empty<string>(),
        "#000000",
        0m,
        0.0,
        string.Empty,
        string.Empty,
        string.Empty,
        null,
        null,
        Array.Empty<string>(),
        Array.Empty<string>());
}
