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

        // Parse colors (tilde-separated slugs)
        if (!string.IsNullOrWhiteSpace(colors))
        {
            var colorSlugs = colors.Split('~', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var slug in colorSlugs.Take(8)) // Max 8 colors
            {
                var colorNumber = RalColor.FromSlug(slug);
                var color = AllColors.FirstOrDefault(c =>
                    c.Number.Equals(colorNumber, StringComparison.OrdinalIgnoreCase) ||
                    c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

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
