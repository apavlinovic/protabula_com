using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Models;
using protabula_com.Services;

public class ConverterModel : PageModel
{
    private readonly IRalColorLoader _loader;

    public ConverterModel(IRalColorLoader loader)
    {
        _loader = loader;
    }

    public RalColor? SelectedColor { get; private set; }
    public IReadOnlyList<RalColor> AllColors { get; private set; } = Array.Empty<RalColor>();
    public ColorFormats? Formats { get; private set; }

    public async Task OnGetAsync(string? ral)
    {
        AllColors = await _loader.LoadAsync();

        if (string.IsNullOrWhiteSpace(ral))
        {
            return;
        }

        // Find matching color by number (case-insensitive)
        SelectedColor = AllColors.FirstOrDefault(c =>
            c.Number.Equals(ral, StringComparison.OrdinalIgnoreCase) ||
            c.Slug.Equals(ral, StringComparison.OrdinalIgnoreCase));

        if (SelectedColor != null)
        {
            Formats = ColorFormats.FromHex(SelectedColor.Hex);
        }
    }
}
