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
        string nameDe)
    {
        Category = category;
        RootColor = rootColor;
        Tags = tags;
        Hex = hex;
        Brightness = brightness;
        Number = number;
        Name = name;
        NameDe = nameDe;
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
        string.Empty);
}
