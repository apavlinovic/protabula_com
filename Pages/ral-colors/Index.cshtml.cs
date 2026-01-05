using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Models;
using protabula_com.Services;

namespace protabula_com.Pages;

public class RalColorsModel : PageModel
{
    private readonly IRalColorLoader _loader;

    public RalColorsModel(IRalColorLoader loader)
    {
        _loader = loader;
    }

    public IReadOnlyList<RalColor> Colors { get; private set; } = Array.Empty<RalColor>();
    public RootColor? SelectedRootColor { get; private set; }
    public IReadOnlyList<RootColor> AvailableRootColors { get; private set; } = Array.Empty<RootColor>();

    public async Task<IActionResult> OnGetAsync(string? color = null)
    {
        // Parse root color filter if provided
        if (!string.IsNullOrEmpty(color))
        {
            if (Enum.TryParse<RootColor>(color, ignoreCase: true, out var parsed)
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

        // Get available root colors (for filter UI)
        AvailableRootColors = allColors
            .Select(c => c.RootColor)
            .Where(r => r != RootColor.Unknown)
            .Distinct()
            .OrderBy(r => r.ToString())
            .ToList();

        // Apply filter if selected
        Colors = SelectedRootColor.HasValue
            ? allColors.Where(c => c.RootColor == SelectedRootColor.Value).ToList()
            : allColors;

        return Page();
    }
}
