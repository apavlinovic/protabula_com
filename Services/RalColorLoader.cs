using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using protabula_com.Models;

namespace protabula_com.Services;

public interface IRalColorLoader
{
    Task<IReadOnlyList<RalColor>> LoadAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RalColor>> LoadByCategoryAsync(RalCategory category, CancellationToken cancellationToken = default);
    Task<RalColor?> LoadSingleAsync(string colorNumber, CancellationToken cancellationToken = default);
}

public sealed class RalColorLoader : IRalColorLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RalColorLoader> _logger;
    private readonly IRootColorClassifier _rootColorClassifier;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IReadOnlyList<RalColor>? _cache;

    public RalColorLoader(
        IWebHostEnvironment environment,
        ILogger<RalColorLoader> logger,
        IRootColorClassifier rootColorClassifier)
    {
        _environment = environment;
        _logger = logger;
        _rootColorClassifier = rootColorClassifier;
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
                var brightness = ParseBrightness(record.Brightness, record.Number);
                var tags = ParseTags(record.Tags);
                var hex = record.Hex ?? string.Empty;
                var rootColor = _rootColorClassifier.Classify(new ColorClassificationContext(
                    Hex: hex,
                    Name: record.Name,
                    Category: category,
                    Number: record.Number
                ));

                colors.Add(new RalColor(
                    category,
                    rootColor,
                    tags,
                    hex,
                    brightness,
                    record.Number ?? string.Empty,
                    record.Name ?? string.Empty,
                    record.NameDe ?? string.Empty,
                    record.DescriptionEn,
                    record.DescriptionDe));
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

    public async Task<RalColor?> LoadSingleAsync(string colorNumber, CancellationToken cancellationToken = default)
    {
        var colors = await LoadAsync(cancellationToken);
        return colors.FirstOrDefault(c => c.Number.Equals(colorNumber, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record RalColorRecord(
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("tags")] string? Tags,
        [property: JsonPropertyName("hex")] string? Hex,
        [property: JsonPropertyName("brightness")] string? Brightness,
        [property: JsonPropertyName("number")] string? Number,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("name_de")] string? NameDe,
        [property: JsonPropertyName("description_en")] string? DescriptionEn,
        [property: JsonPropertyName("description_de")] string? DescriptionDe);
}
