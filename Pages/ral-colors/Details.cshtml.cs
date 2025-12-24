using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Models;
using protabula_com.Services;

public class RalColorDetailsModel : PageModel
{
    private readonly IRalColorLoader _loader;
    private readonly ISimilarColorFinder _similarColorFinder;

    public RalColorDetailsModel(IRalColorLoader loader, ISimilarColorFinder similarColorFinder)
    {
        _loader = loader;
        _similarColorFinder = similarColorFinder;
        Color = RalColor.Empty;
        SimilarColors = new SimilarColorsResult();
    }

    public string? ColorIdentifier { get; set; }
    public RalColor Color { get; set; }
    public SimilarColorsResult SimilarColors { get; set; }
    public ColorFormats? Formats { get; private set; }

    public async Task OnGetAsync(string colorIdentifier)
    {
        if (string.IsNullOrWhiteSpace(colorIdentifier))
        {
            Color = RalColor.Empty;
            ColorIdentifier = colorIdentifier;
            return;
        }

        ColorIdentifier = colorIdentifier;

        // Convert URL slug (underscores) back to color number format (spaces)
        var colorNumber = RalColor.FromSlug(colorIdentifier);
        Color = await _loader.LoadSingleAsync(colorNumber) ?? RalColor.Empty;

        if (Color != RalColor.Empty)
        {
            Formats = ColorFormats.FromHex(Color.Hex);
            var allColors = await _loader.LoadAsync();
            SimilarColors = _similarColorFinder.FindSimilar(Color, allColors, maxPerCategory: 10);
        }
    }
}
