using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Models;
using protabula_com.Services;

public class CompareModel : PageModel
{
    private readonly IRalColorLoader _loader;

    public CompareModel(IRalColorLoader loader)
    {
        _loader = loader;
    }

    public IReadOnlyList<RalColor> AllColors { get; private set; } = Array.Empty<RalColor>();
    public List<RalColor> SelectedColors { get; private set; } = new();
    public string Background { get; private set; } = "white";

    public async Task OnGetAsync(string? colors, string? bg)
    {
        AllColors = await _loader.LoadAsync();

        // Parse background
        if (!string.IsNullOrWhiteSpace(bg))
        {
            Background = bg.ToLowerInvariant();
        }

        // Parse colors (comma-separated)
        if (!string.IsNullOrWhiteSpace(colors))
        {
            var colorNumbers = colors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var number in colorNumbers.Take(8)) // Max 8 colors
            {
                var color = AllColors.FirstOrDefault(c =>
                    c.Number.Equals(number, StringComparison.OrdinalIgnoreCase) ||
                    c.Slug.Equals(number, StringComparison.OrdinalIgnoreCase));

                if (color != null)
                {
                    SelectedColors.Add(color);
                }
            }
        }
    }

    public string GetBackgroundHex() => Background switch
    {
        "white" => "#FFFFFF",
        "light-grey" => "#E0E0E0",
        "grey" => "#9E9E9E",
        "dark-grey" => "#424242",
        "black" => "#000000",
        _ => "#FFFFFF"
    };
}
