using System.Globalization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace protabula_com.Localization;

/// <summary>
/// Custom HtmlLocalizer that properly uses IStringLocalizer's argument-aware indexer
/// instead of doing its own formatting (which silently catches FormatException).
/// </summary>
public sealed class JsonHtmlLocalizer : IHtmlLocalizer
{
    private readonly IStringLocalizer _localizer;

    public JsonHtmlLocalizer(IStringLocalizer localizer)
    {
        _localizer = localizer;
    }

    public LocalizedHtmlString this[string name]
    {
        get
        {
            var result = _localizer[name];
            return new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound);
        }
    }

    public LocalizedHtmlString this[string name, params object[] arguments]
    {
        get
        {
            // Call the IStringLocalizer indexer WITH arguments.
            // JsonStringLocalizer[name, args] does string.Format internally and throws on error.
            // By passing no arguments to LocalizedHtmlString, its Value property just returns
            // the already-formatted string without trying to format again.
            var result = _localizer[name, arguments];
            return new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound);
        }
    }

    public LocalizedString GetString(string name)
    {
        return _localizer[name];
    }

    public LocalizedString GetString(string name, params object[] arguments)
    {
        return _localizer[name, arguments];
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _localizer.GetAllStrings(includeParentCultures);
    }
}
