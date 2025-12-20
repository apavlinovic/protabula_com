using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using protabula_com.Models;

namespace protabula_com.Services;

public interface IRalColorLoader
{
    Task<IReadOnlyList<RalColor>> LoadAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RalColor>> LoadByCategoryAsync(RalCategory category, CancellationToken cancellationToken = default);
}

public sealed class RalColorLoader : IRalColorLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RalColorLoader> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IReadOnlyList<RalColor>? _cache;

    public RalColorLoader(IWebHostEnvironment environment, ILogger<RalColorLoader> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RalColor>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null)
            {
                return _cache;
            }

            var path = Path.Combine(_environment.ContentRootPath, "Data", "RAL", "all-colors.json");
            if (!File.Exists(path))
            {
                _logger.LogWarning("RAL color file not found at {Path}", path);
                _cache = Array.Empty<RalColor>();
                return _cache;
            }

            var json = await File.ReadAllTextAsync(path, cancellationToken);
            var records = JsonSerializer.Deserialize<List<RalColorRecord>>(json, JsonOptions) ?? new();

            var colors = new List<RalColor>(records.Count);
            foreach (var record in records)
            {
                if (record is null)
                {
                    continue;
                }

                var category = ParseCategory(record.Category, record.Number);
                var colorCategory = ParseColorCategory(record.Hex);
                var brightness = ParseBrightness(record.Brightness, record.Number);
                var tags = ParseTags(record.Tags);

                colors.Add(new RalColor(
                    category,
                    colorCategory.ToString(),
                    tags,
                    record.Hex ?? string.Empty,
                    brightness,
                    record.Number ?? string.Empty,
                    record.Name ?? string.Empty,
                    record.NameDe ?? string.Empty));
            }

            _cache = colors;
            return _cache;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<IReadOnlyList<RalColor>> LoadByCategoryAsync(RalCategory category, CancellationToken cancellationToken = default)
    {
        var colors = await LoadAsync(cancellationToken);
        return colors.Where(color => color.Category == category).ToArray();
    }

    private decimal ParseBrightness(string? value, string? number)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var brightness))
        {
            return brightness;
        }

        _logger.LogWarning("Unable to parse brightness value {Brightness} for RAL {Number}", value, number ?? "(unknown)");
        return 0m;
    }

    private ColorCategory ParseColorCategory(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return ColorCategory.Unknown;
        }

        try
        {
            var result = ColorClassifier.Classify(hex);
            return result.Category;
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Unable to parse hex color {Hex}", hex);
            return ColorCategory.Unknown;
        }
    }

    private RalCategory ParseCategory(string? value, string? number)
    {
        return value switch
        {
            "ral-classic" => RalCategory.Classic,
            "ral-design-system-plus" => RalCategory.DesignPlus,
            "ral-effect" => RalCategory.Effect,
            _ => LogUnknownCategory(value, number)
        };
    }

    private RalCategory LogUnknownCategory(string? value, string? number)
    {
        _logger.LogWarning("Unknown RAL category {Category} for RAL {Number}", value ?? "(null)", number ?? "(unknown)");
        return RalCategory.Classic;
    }

    private static IReadOnlyList<string> ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return Array.Empty<string>();
        }

        return tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private sealed record RalColorRecord(
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("tags")] string? Tags,
        [property: JsonPropertyName("hex")] string? Hex,
        [property: JsonPropertyName("brightness")] string? Brightness,
        [property: JsonPropertyName("number")] string? Number,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("name_de")] string? NameDe);
}
