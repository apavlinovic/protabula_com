namespace protabula_com.Models;

public enum RalCategory
{
    Classic,
    DesignPlus,
    Effect
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
    }

    public RalCategory Category { get; }
    public bool NeedsDarkText
    {
        get
        {
            return Tags.Contains("dark");
        }
    }
    public IReadOnlyList<string> Tags { get; }
    public string Hex { get; }
    public decimal Brightness { get; }
    public string Number { get; }
    public string Name { get; }
    public string NameDe { get; }
}
