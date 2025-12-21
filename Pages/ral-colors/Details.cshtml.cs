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

    public async Task OnGetAsync(string colorIdentifier)
    {
        if (string.IsNullOrWhiteSpace(colorIdentifier))
        {
            Color = RalColor.Empty;
            ColorIdentifier = colorIdentifier;
            return;
        }

        ColorIdentifier = colorIdentifier;

        // Convert URL slug (dashes) back to color number format (spaces)
        var colorNumber = colorIdentifier.Replace('-', ' ');
        Color = await _loader.LoadSingleAsync(colorNumber) ?? RalColor.Empty;

        if (Color != RalColor.Empty)
        {
            var allColors = await _loader.LoadAsync();
            SimilarColors = _similarColorFinder.FindSimilar(Color, allColors);
        }
    }
}
