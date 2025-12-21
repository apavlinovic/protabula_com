using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace protabula_com.Localization;

public sealed class JsonStringLocalizer : IStringLocalizer
{
    private const string SharedFileName = "_Shared";

    // Cache per file path so repeated lookups avoid re-reading disk.
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> FileCache = new(StringComparer.OrdinalIgnoreCase);

    // Cache the list of shared resource paths for each base name.
    private static readonly ConcurrentDictionary<string, IReadOnlyList<string>> SharedPathsCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _contentRootPath;
    private readonly string _resourcesPath;
    private readonly string _baseName;
    private readonly IReadOnlyList<string> _sharedBaseNames;
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
        _sharedBaseNames = GetSharedResourcePaths(baseName, contentRootPath, resourcesPath);
        _logger = logger;
    }

    /// <summary>
    /// Builds a list of _Shared resource paths by walking up the folder hierarchy.
    /// For "Pages.ral-colors.Compare", returns:
    ///   - "Pages.ral-colors._Shared"
    ///   - "Pages._Shared"
    ///   - "_Shared"
    /// </summary>
    private static IReadOnlyList<string> GetSharedResourcePaths(
        string baseName,
        string contentRootPath,
        string resourcesPath)
    {
        return SharedPathsCache.GetOrAdd(baseName, name =>
        {
            var paths = new List<string>();
            var parts = name.Split('.');

            // Walk up the hierarchy, looking for _Shared at each level
            // Start from the immediate parent folder, not the file itself
            for (var i = parts.Length - 1; i >= 1; i--)
            {
                var parentPath = string.Join(".", parts.Take(i));
                var sharedPath = string.IsNullOrEmpty(parentPath)
                    ? SharedFileName
                    : $"{parentPath}.{SharedFileName}";
                paths.Add(sharedPath);
            }

            // Add root-level _Shared
            paths.Add(SharedFileName);

            return paths;
        });
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetValue(name, CultureInfo.CurrentUICulture);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = GetValue(name, CultureInfo.CurrentUICulture) ?? name;
            var value = string.Format(CultureInfo.CurrentUICulture, format, arguments);
            return new LocalizedString(name, value, resourceNotFound: format == name);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture;
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // First return page-specific resources
        foreach (var entry in LoadResources(culture, _baseName))
        {
            if (seenKeys.Add(entry.Key))
            {
                yield return new LocalizedString(entry.Key, entry.Value, resourceNotFound: false);
            }
        }

        // Then shared resources (in order from most specific to least)
        foreach (var sharedBaseName in _sharedBaseNames)
        {
            foreach (var entry in LoadResources(culture, sharedBaseName))
            {
                if (seenKeys.Add(entry.Key))
                {
                    yield return new LocalizedString(entry.Key, entry.Value, resourceNotFound: false);
                }
            }
        }

        if (!includeParentCultures)
        {
            yield break;
        }

        var parent = culture.Parent;
        while (parent != CultureInfo.InvariantCulture)
        {
            foreach (var entry in LoadResources(parent, _baseName))
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

        // Walk up the hierarchy looking for _Shared files
        foreach (var sharedBaseName in _sharedBaseNames)
        {
            value = GetValueFromBaseName(key, culture, sharedBaseName);
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

    private Dictionary<string, string> LoadResources(CultureInfo culture, string baseName)
    {
        var resourcePath = BuildResourcePath(culture, baseName);
        if (resourcePath is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return FileCache.GetOrAdd(resourcePath, path =>
        {
            if (!File.Exists(path))
            {
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
