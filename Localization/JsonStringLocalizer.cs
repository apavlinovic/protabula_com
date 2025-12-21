using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace protabula_com.Localization;

public sealed class JsonStringLocalizer : IStringLocalizer
{
    // Cache per file path so repeated lookups avoid re-reading disk.
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _contentRootPath;
    private readonly string _resourcesPath;
    private readonly string _baseName;
    private readonly string? _sharedBaseName;
    private readonly ILogger<JsonStringLocalizer> _logger;

    public JsonStringLocalizer(
        string contentRootPath,
        string resourcesPath,
        string baseName,
        ILogger<JsonStringLocalizer> logger)
    {
        _contentRootPath = contentRootPath;
        _resourcesPath = resourcesPath;
        _baseName = baseName;
        _sharedBaseName = DetectSharedResourceName(baseName);
        _logger = logger;
    }

    /// <summary>
    /// Determines the shared resource file to use based on the page path.
    /// For pages under "Pages.ral-colors.*" or color-related partials, uses "Shared.RalColors".
    /// </summary>
    private static string? DetectSharedResourceName(string baseName)
    {
        if (baseName.StartsWith("Pages.ral-colors", StringComparison.OrdinalIgnoreCase))
        {
            return "Shared.RalColors";
        }

        // Color-related partials also need access to shared RAL resources
        if (baseName.Contains("ColorAutocomplete", StringComparison.OrdinalIgnoreCase))
        {
            return "Shared.RalColors";
        }

        return null;
    }

    public LocalizedString this[string name]
    {
        get
        {
            // Basic lookup without formatting.
            var value = GetValue(name, CultureInfo.CurrentUICulture);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            // Uses string.Format with the current UI culture.
            var format = GetValue(name, CultureInfo.CurrentUICulture) ?? name;
            var value = string.Format(CultureInfo.CurrentUICulture, format, arguments);
            return new LocalizedString(name, value, resourceNotFound: format == name);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture;
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resources = LoadResources(culture);
        foreach (var entry in resources)
        {
            if (seenKeys.Add(entry.Key))
            {
                yield return new LocalizedString(entry.Key, entry.Value, resourceNotFound: false);
            }
        }

        if (!includeParentCultures)
        {
            yield break;
        }

        var parent = culture.Parent;
        while (parent != CultureInfo.InvariantCulture)
        {
            foreach (var entry in LoadResources(parent))
            {
                if (seenKeys.Add(entry.Key))
                {
                    yield return new LocalizedString(entry.Key, entry.Value, resourceNotFound: false);
                }
            }

            parent = parent.Parent;
        }
    }

    private string? GetValue(string key, CultureInfo culture)
    {
        // Try page-specific resources first
        var value = GetValueFromBaseName(key, culture, _baseName);
        if (value != null)
        {
            return value;
        }

        // Fall back to shared resources if available
        if (_sharedBaseName != null)
        {
            value = GetValueFromBaseName(key, culture, _sharedBaseName);
            if (value != null)
            {
                return value;
            }
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Missing localization key {Key} for {BaseName} in culture {Culture}",
                key,
                _baseName,
                CultureInfo.CurrentUICulture.Name);
        }

        return null;
    }

    private string? GetValueFromBaseName(string key, CultureInfo culture, string baseName)
    {
        // Try exact culture first (e.g., de-DE).
        var resources = LoadResources(culture, baseName);
        if (resources.TryGetValue(key, out var value))
        {
            return value;
        }

        if (!string.IsNullOrEmpty(culture.Name))
        {
            // Then neutral culture (e.g., de).
            var neutralName = culture.TwoLetterISOLanguageName;
            if (!string.IsNullOrWhiteSpace(neutralName) && !string.Equals(neutralName, culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                var neutralCulture = CultureInfo.GetCultureInfo(neutralName);
                var neutralResources = LoadResources(neutralCulture, baseName);
                if (neutralResources.TryGetValue(key, out value))
                {
                    return value;
                }
            }
        }

        // Finally fall back to invariant (no culture suffix).
        var invariantResources = LoadResources(CultureInfo.InvariantCulture, baseName);
        if (invariantResources.TryGetValue(key, out value))
        {
            return value;
        }

        return null;
    }

    private Dictionary<string, string> LoadResources(CultureInfo culture) =>
        LoadResources(culture, _baseName);

    private Dictionary<string, string> LoadResources(CultureInfo culture, string baseName)
    {
        // Map the base name and culture into a file path under ResourcesJson/.
        var resourcePath = BuildResourcePath(culture, baseName);
        if (resourcePath is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return Cache.GetOrAdd(resourcePath, path =>
        {
            if (!File.Exists(path))
            {
                // Missing file is normal (e.g., no de-DE file yet).
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Localization file not found at {Path}", path);
                }
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var json = File.ReadAllText(path);
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException exception)
            {
                // Invalid JSON should not break a request.
                _logger.LogWarning(exception, "Invalid JSON localization file at {Path}", path);
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        });
    }

    private string? BuildResourcePath(CultureInfo culture, string baseName)
    {
        var basePath = baseName.Replace('.', Path.DirectorySeparatorChar);
        var resourcesRoot = Path.Combine(_contentRootPath, _resourcesPath);

        if (culture == CultureInfo.InvariantCulture)
        {
            return Path.Combine(resourcesRoot, basePath + ".json");
        }

        var cultureName = culture.Name;
        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            var path = Path.Combine(resourcesRoot, basePath + "." + cultureName + ".json");
            if (File.Exists(path))
            {
                return path;
            }
        }

        var neutralName = culture.TwoLetterISOLanguageName;
        if (!string.IsNullOrWhiteSpace(neutralName))
        {
            var neutralPath = Path.Combine(resourcesRoot, basePath + "." + neutralName + ".json");
            if (File.Exists(neutralPath))
            {
                return neutralPath;
            }
        }

        return Path.Combine(resourcesRoot, basePath + ".json");
    }
}
