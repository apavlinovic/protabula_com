using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace protabula_com.Localization;

public sealed class JsonStringLocalizer : IStringLocalizer
{
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _contentRootPath;
    private readonly string _resourcesPath;
    private readonly string _baseName;

    public JsonStringLocalizer(string contentRootPath, string resourcesPath, string baseName)
    {
        _contentRootPath = contentRootPath;
        _resourcesPath = resourcesPath;
        _baseName = baseName;
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
        var resources = LoadResources(culture);
        foreach (var entry in resources)
        {
            yield return new LocalizedString(entry.Key, entry.Value, resourceNotFound: false);
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
                yield return new LocalizedString(entry.Key, entry.Value, resourceNotFound: false);
            }

            parent = parent.Parent;
        }
    }

    private string? GetValue(string key, CultureInfo culture)
    {
        var resources = LoadResources(culture);
        if (resources.TryGetValue(key, out var value))
        {
            return value;
        }

        if (!string.IsNullOrEmpty(culture.Name))
        {
            var neutralResources = LoadResources(CultureInfo.InvariantCulture);
            if (neutralResources.TryGetValue(key, out value))
            {
                return value;
            }
        }

        return null;
    }

    private Dictionary<string, string> LoadResources(CultureInfo culture)
    {
        var resourcePath = BuildResourcePath(culture);
        if (resourcePath is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return Cache.GetOrAdd(resourcePath, path =>
        {
            if (!File.Exists(path))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(path);
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        });
    }

    private string? BuildResourcePath(CultureInfo culture)
    {
        var basePath = _baseName.Replace('.', Path.DirectorySeparatorChar);
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
