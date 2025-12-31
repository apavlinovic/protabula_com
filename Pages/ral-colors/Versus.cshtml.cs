using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Helpers;
using protabula_com.Models;
using protabula_com.Services;

public class VersusModel : PageModel
{
    private readonly IRalColorLoader _loader;
    private readonly IColorImageService _colorImageService;

    public VersusModel(IRalColorLoader loader, IColorImageService colorImageService)
    {
        _loader = loader;
        _colorImageService = colorImageService;
        Color1 = RalColor.Empty;
        Color2 = RalColor.Empty;
    }

    public RalColor Color1 { get; set; }
    public RalColor Color2 { get; set; }
    public ColorFormats? Formats1 { get; private set; }
    public ColorFormats? Formats2 { get; private set; }

    // Color comparison metrics
    public double DeltaE { get; private set; }
    public string DeltaEInterpretation { get; private set; } = "";

    // Color temperature for each color
    public (int Kelvin, string Classification) Temperature1 { get; private set; }
    public (int Kelvin, string Classification) Temperature2 { get; private set; }

    // Scene images
    public IReadOnlyList<string> Scenes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        // Parse "7016-vs-7035" or "000_15_00-vs-7016"
        var parts = slug.Split("-vs-", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return NotFound();
        }

        // Convert URL slugs (underscores) back to color number format (spaces)
        var colorNumber1 = RalColor.FromSlug(parts[0]);
        var colorNumber2 = RalColor.FromSlug(parts[1]);

        Color1 = await _loader.LoadSingleAsync(colorNumber1) ?? RalColor.Empty;
        Color2 = await _loader.LoadSingleAsync(colorNumber2) ?? RalColor.Empty;

        if (Color1 == RalColor.Empty || Color2 == RalColor.Empty)
        {
            return NotFound();
        }

        Formats1 = ColorFormats.FromHex(Color1.Hex);
        Formats2 = ColorFormats.FromHex(Color2.Hex);

        // Calculate comparison metrics
        DeltaE = ColorMath.GetDeltaE(Color1.Hex, Color2.Hex);
        DeltaEInterpretation = ColorMath.GetDeltaEInterpretation(DeltaE);

        Temperature1 = ColorMath.EstimateColorTemperature(Color1.Hex);
        Temperature2 = ColorMath.EstimateColorTemperature(Color2.Hex);

        Scenes = _colorImageService.GetValidScenes();

        return Page();
    }

    public string GetColor1Title()
    {
        return string.IsNullOrEmpty(Color1.Name)
            ? $"RAL {Color1.Number}"
            : $"RAL {Color1.Number} {Color1.Name}";
    }

    public string GetColor2Title()
    {
        return string.IsNullOrEmpty(Color2.Name)
            ? $"RAL {Color2.Number}"
            : $"RAL {Color2.Number} {Color2.Name}";
    }
}
