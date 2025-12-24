using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace protabula_com.Localization;

/// <summary>
/// Factory that creates JsonHtmlLocalizer instances using the custom IStringLocalizerFactory.
/// </summary>
public sealed class JsonHtmlLocalizerFactory : IHtmlLocalizerFactory
{
    private readonly IStringLocalizerFactory _stringLocalizerFactory;

    public JsonHtmlLocalizerFactory(IStringLocalizerFactory stringLocalizerFactory)
    {
        _stringLocalizerFactory = stringLocalizerFactory;
    }

    public IHtmlLocalizer Create(Type resourceSource)
    {
        var stringLocalizer = _stringLocalizerFactory.Create(resourceSource);
        return new JsonHtmlLocalizer(stringLocalizer);
    }

    public IHtmlLocalizer Create(string baseName, string location)
    {
        var stringLocalizer = _stringLocalizerFactory.Create(baseName, location);
        return new JsonHtmlLocalizer(stringLocalizer);
    }
}
