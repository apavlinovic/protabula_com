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

    public async Task<IActionResult> OnGetAsync(string category)
    {
        Category = ParseCategory(category);

        if (Category == null)
        {
            return NotFound();
        }

        var allColors = await _loader.LoadAsync();
        Colors = allColors.Where(c => c.Category == Category.Value).ToList();

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
