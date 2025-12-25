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

public sealed class RalColor
{
    public RalColor(
        RalCategory category,
        RootColor rootColor,
        IReadOnlyList<string> tags,
        string hex,
        decimal brightness,
        string number,
        string name,
        string nameDe,
        string? descriptionEn = null,
        string? descriptionDe = null)
    {
        Category = category;
        RootColor = rootColor;
        Tags = tags;
        Hex = hex;
        Brightness = brightness;
        Number = number;
        Name = name;
        NameDe = nameDe;
        DescriptionEn = descriptionEn;
        DescriptionDe = descriptionDe;
    }

    public RalCategory Category { get; }
    public RootColor RootColor { get; }

    public bool NeedsDarkText => Tags.Any(tag => tag.Equals("dark", StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> Tags { get; }
    public string Hex { get; }
    public decimal Brightness { get; }
    public string Number { get; }
    public string Name { get; }
    public string NameDe { get; }
    public string? DescriptionEn { get; }
    public string? DescriptionDe { get; }

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
        string.Empty,
        string.Empty,
        string.Empty,
        null,
        null);
}
