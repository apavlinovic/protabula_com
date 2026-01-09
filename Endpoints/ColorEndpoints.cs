using protabula_com.Helpers;
using protabula_com.Models;
using protabula_com.Services;

namespace protabula_com.Endpoints;

public static class ColorEndpoints
{
    public static IEndpointRouteBuilder MapColorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // RAL color matching API endpoint - finds closest RAL colors to an RGB value
        endpoints.MapGet("/api/ral/match", async (int r, int g, int b, string? culture, IRalColorLoader colorLoader) =>
        {
            // Clamp values to valid range
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);

            var lang = culture == "de" ? "de" : "en";
            var hex = $"#{r:X2}{g:X2}{b:X2}";
            var inputLab = ColorMath.HexToLab(hex);
            var allColors = await colorLoader.LoadAsync();

            // Use CIEDE2000 for perceptually accurate color matching
            var ciede2000 = new Colourful.CIEDE2000ColorDifference();

            // Helper to create match object
            object CreateMatch(RalColor c, double deltaE) => new
            {
                number = c.Number,
                name = c.GetLocalizedName(lang),
                hex = c.Hex,
                category = c.Category.ToString(),
                slug = c.Slug,
                needsDarkText = c.NeedsDarkText,
                deltaE = Math.Round(deltaE, 2)
            };

            // Get top 5 Classic matches
            var classicMatches = allColors
                .Where(c => c.Category == RalCategory.Classic)
                .Select(c => (color: c, deltaE: ciede2000.ComputeDifference(inputLab, ColorMath.HexToLab(c.Hex))))
                .OrderBy(x => x.deltaE)
                .Take(5)
                .Select(x => CreateMatch(x.color, x.deltaE))
                .ToList();

            // Get top 5 Design Plus matches
            var designMatches = allColors
                .Where(c => c.Category == RalCategory.DesignPlus)
                .Select(c => (color: c, deltaE: ciede2000.ComputeDifference(inputLab, ColorMath.HexToLab(c.Hex))))
                .OrderBy(x => x.deltaE)
                .Take(5)
                .Select(x => CreateMatch(x.color, x.deltaE))
                .ToList();

            return Results.Ok(new { input = hex, r, g, b, classic = classicMatches, design = designMatches });
        });

        // Color search API endpoint - returns results grouped by category
        endpoints.MapGet("/api/colors/search", async (string? q, string? culture, IRalColorLoader colorLoader) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Results.Ok(new { categories = Array.Empty<object>() });
            }

            var lang = culture == "de" ? "de" : "en";
            var colors = await colorLoader.LoadAsync();
            var query = q.Trim();

            var matchingColors = colors
                .Where(c =>
                    c.Number.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.GetLocalizedName(lang).Contains(query, StringComparison.OrdinalIgnoreCase));

            var categoryNames = new Dictionary<RalCategory, (string key, string en, string de)>
            {
                { RalCategory.Classic, ("classic", "RAL Classic", "RAL Classic") },
                { RalCategory.DesignPlus, ("design-plus", "RAL Design System Plus", "RAL Design System Plus") },
                { RalCategory.Effect, ("effect", "RAL Effect", "RAL Effect") }
            };

            var categories = matchingColors
                .GroupBy(c => c.Category)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    key = categoryNames[g.Key].key,
                    name = lang == "de" ? categoryNames[g.Key].de : categoryNames[g.Key].en,
                    colors = g.Take(10).Select(c => new
                    {
                        number = c.Number,
                        name = c.GetLocalizedName(lang),
                        hex = c.Hex,
                        slug = c.Slug,
                        needsDarkText = c.NeedsDarkText
                    }).ToArray()
                })
                .Where(c => c.colors.Length > 0)
                .ToArray();

            return Results.Ok(new { categories });
        });

        return endpoints;
    }
}
