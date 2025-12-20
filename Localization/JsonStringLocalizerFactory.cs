using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace protabula_com.Localization;

public sealed class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly string _resourcesPath;
    private readonly string _contentRootPath;

    public JsonStringLocalizerFactory(IOptions<LocalizationOptions> localizationOptions, IWebHostEnvironment environment)
    {
        _resourcesPath = localizationOptions.Value.ResourcesPath ?? string.Empty;
        _contentRootPath = environment.ContentRootPath;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var baseName = resourceSource.FullName ?? resourceSource.Name;
        var location = resourceSource.Assembly.GetName().Name ?? string.Empty;
        return Create(baseName, location);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        if (!string.IsNullOrWhiteSpace(location) && baseName.StartsWith(location + ".", StringComparison.Ordinal))
        {
            baseName = baseName[(location.Length + 1)..];
        }

        return new JsonStringLocalizer(_contentRootPath, _resourcesPath, baseName);
    }
}
