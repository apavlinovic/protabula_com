using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace protabula_com.Localization;

public sealed class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly string _resourcesPath;
    private readonly string _contentRootPath;
    private readonly ILoggerFactory _loggerFactory;

    public JsonStringLocalizerFactory(
        IOptions<LocalizationOptions> localizationOptions,
        IWebHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        _resourcesPath = localizationOptions.Value.ResourcesPath ?? string.Empty;
        _contentRootPath = environment.ContentRootPath;
        _loggerFactory = loggerFactory;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var baseName = resourceSource.FullName ?? resourceSource.Name;
        var location = resourceSource.Assembly.GetName().Name ?? string.Empty;
        return Create(baseName, location);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        // Trim the assembly prefix so baseName matches folder layout under ResourcesJson/.
        if (!string.IsNullOrWhiteSpace(location) && baseName.StartsWith(location + ".", StringComparison.Ordinal))
        {
            baseName = baseName[(location.Length + 1)..];
        }

        // Each view gets a localizer rooted at its baseName path.
        var logger = _loggerFactory.CreateLogger<JsonStringLocalizer>();
        return new JsonStringLocalizer(_contentRootPath, _resourcesPath, baseName, logger);
    }
}
