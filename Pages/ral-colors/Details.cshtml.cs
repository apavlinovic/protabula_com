using Microsoft.AspNetCore.Mvc.RazorPages;
using protabula_com.Models;
using protabula_com.Services;

public class RalColorDetailsModel : PageModel
{
    private readonly IRalColorLoader _loader;
    public RalColorDetailsModel(IRalColorLoader loader)
    {
        _loader = loader;
        Color = RalColor.Empty;
    }


    public string? ColorIdentifier { get; set; }
    public RalColor Color { get; set; }

    public async Task OnGetAsync(string colorIdentifier)
    {
        if (string.IsNullOrWhiteSpace(colorIdentifier))
        {
            Color = RalColor.Empty;
            ColorIdentifier = colorIdentifier;
            return;
        }

        ColorIdentifier = colorIdentifier;
        Color = await _loader.LoadSingleAsync(colorIdentifier);
    }
}
