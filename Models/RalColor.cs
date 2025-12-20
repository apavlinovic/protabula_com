namespace protabula_com.Models;

public enum RalCategory
{
    Classic,
    DesignPlus,
    Effect
}

public sealed record RalColor(
    RalCategory Category,
    string ColorCategory,
    IReadOnlyList<string> Tags,
    string Hex,
    decimal Brightness,
    string Number,
    string Name,
    string NameDe);
