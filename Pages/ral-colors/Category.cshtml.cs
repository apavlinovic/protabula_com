using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Models;
using protabula_com.Services;

public class CategoryModel : PageModel
{
    private readonly IRalColorLoader _loader;

    public CategoryModel(IRalColorLoader loader)
    {
        _loader = loader;
    }

    public RalCategory? Category { get; private set; }
    public IReadOnlyList<RalColor> Colors { get; private set; } = Array.Empty<RalColor>();
    public RootColor? SelectedRootColor { get; private set; }
    public IReadOnlyList<RootColor> AvailableRootColors { get; private set; } = Array.Empty<RootColor>();

    public async Task<IActionResult> OnGetAsync(string category, string? rootColor = null)
    {
        Category = ParseCategory(category);

        if (Category == null)
        {
            return NotFound();
        }

        // Parse rootColor if provided
        if (!string.IsNullOrEmpty(rootColor))
        {
            if (Enum.TryParse<RootColor>(rootColor, ignoreCase: true, out var parsed)
                && parsed != RootColor.Unknown)
            {
                SelectedRootColor = parsed;
            }
            else
            {
                return NotFound();
            }
        }

        var allColors = await _loader.LoadAsync();
        var categoryColors = allColors.Where(c => c.Category == Category.Value).ToList();

        // Get available root colors in this category (for UI)
        AvailableRootColors = categoryColors
            .Select(c => c.RootColor)
            .Where(r => r != RootColor.Unknown)
            .Distinct()
            .OrderBy(r => r.GetDisplayOrder())
            .ToList();

        // Apply root color filter if selected
        Colors = SelectedRootColor.HasValue
            ? categoryColors.Where(c => c.RootColor == SelectedRootColor.Value).ToList()
            : categoryColors;

        return Page();
    }

    private static RalCategory? ParseCategory(string? slug) => slug?.ToLowerInvariant() switch
    {
        "classic" => RalCategory.Classic,
        "design-plus" => RalCategory.DesignPlus,
        "effect" => RalCategory.Effect,
        _ => null
    };

    public static string GetCategorySlug(RalCategory category) => category switch
    {
        RalCategory.Classic => "classic",
        RalCategory.DesignPlus => "design-plus",
        RalCategory.Effect => "effect",
        _ => ""
    };
}
