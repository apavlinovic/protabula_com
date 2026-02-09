using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Models;
using protabula_com.Services;

public class GuideModel : PageModel
{
    private readonly IRalColorLoader _loader;

    public GuideModel(IRalColorLoader loader)
    {
        _loader = loader;
    }

    public List<RalColor> PopularWhites { get; set; } = new();
    public List<RalColor> PopularNeutrals { get; set; } = new();
    public List<RalColor> PopularAccents { get; set; } = new();

    public string CurrentCulture => CultureInfo.CurrentUICulture.Name;

    private static readonly string[] WhiteNumbers = ["9010", "9016", "9001", "9003"];
    private static readonly string[] NeutralNumbers = ["7016", "9005", "7035", "7040"];
    private static readonly string[] AccentNumbers = ["3020", "5015", "1021", "6005"];

    public async Task OnGetAsync()
    {
        var allColors = await _loader.LoadAsync();
        var lookup = allColors.ToDictionary(c => c.Number);

        PopularWhites = ResolveColors(lookup, WhiteNumbers);
        PopularNeutrals = ResolveColors(lookup, NeutralNumbers);
        PopularAccents = ResolveColors(lookup, AccentNumbers);
    }

    private static List<RalColor> ResolveColors(Dictionary<string, RalColor> lookup, string[] numbers)
    {
        var result = new List<RalColor>();
        foreach (var num in numbers)
        {
            if (lookup.TryGetValue(num, out var color))
                result.Add(color);
        }
        return result;
    }
}
