using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Helpers;
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
        SimilarColors = [];
        SameRootColors = [];
        MoodSimilarColors = [];
        LightingVariations = [];
        DirectSunlightVariations = [];
    }

    public string? ColorIdentifier { get; set; }
    public RalColor Color { get; set; }
    public IReadOnlyList<SimilarColor> SimilarColors { get; set; }
    public IReadOnlyList<RalColor> SameRootColors { get; set; }
    public IReadOnlyList<MoodSimilarColor> MoodSimilarColors { get; set; }
    public ColorFormats? Formats { get; private set; }
    public (int Kelvin, string Classification) Temperature { get; private set; }
    public IReadOnlyList<LightingVariation> LightingVariations { get; private set; }
    public IReadOnlyList<DirectSunlightVariation> DirectSunlightVariations { get; private set; }

    // Computed properties to reduce inline C# in the view
    public string CurrentCulture => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    public string LocalizedName => Color.GetLocalizedName(CurrentCulture);
    public string ColorTitle => string.IsNullOrEmpty(LocalizedName)
        ? $"RAL {Color.Number}"
        : $"RAL {Color.Number} ({LocalizedName})";

    public string CategorySlug => Color.Category switch
    {
        RalCategory.Classic => "classic",
        RalCategory.DesignPlus => "design-plus",
        RalCategory.Effect => "effect",
        _ => "classic"
    };

    public int Hue => Formats?.Hsl.H ?? 0;
    public int Saturation => Formats?.Hsl.S ?? 0;
    public double Lrv => Formats?.Lrv ?? 0;
    public LrvLevel LrvLevel => ColorMath.ClassifyLrv(Lrv);

    public bool IsNeutralFamily => Color.RootColor is RootColor.Grey or RootColor.White
        or RootColor.Black or RootColor.Beige;

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
            Formats = ColorFormats.FromRalColor(Color);
            Temperature = ColorMath.EstimateColorTemperature(Color.Hex);
            LightingVariations = LightingSimulator.GenerateUndertoneAwareVariations(Color.Hex);
            DirectSunlightVariations = LightingSimulator.GenerateUndertoneAwareDirectSunlightVariations(Color.Hex);
            var allColors = await _loader.LoadAsync();
            SimilarColors = _similarColorFinder.FindSimilarInCategory(Color, allColors, maxCount: 10);
            SameRootColors = _similarColorFinder.FindSameRootColorInCategory(Color, allColors);
            MoodSimilarColors = _similarColorFinder.FindSimilarByMood(Color, allColors, minSharedTags: 2, maxCount: 8);
        }
    }
}
