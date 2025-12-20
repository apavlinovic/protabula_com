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

    public async Task OnGetAsync()
    {
        Colors = await _loader.LoadAsync();
    }
}
