using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;

namespace protabula_com.Middleware;

public class IpBlocklistOptions
{
    public const string SectionName = "IpBlocklist";

    /// <summary>
    /// List of blocked IP addresses (exact match)
    /// </summary>
    public List<string> BlockedIps { get; set; } = new();

    /// <summary>
    /// List of blocked country codes (2-letter ISO codes from Cloudflare CF-IPCountry header)
    /// </summary>
    public List<string> BlockedCountries { get; set; } = new();

    /// <summary>
    /// Number of rate limit violations before auto-blocking an IP
    /// </summary>
    public int ViolationsBeforeBlock { get; set; } = 10;

    /// <summary>
    /// Duration in minutes to auto-block an IP after exceeding violations
    /// </summary>
    public int AutoBlockDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Enable logging of blocked requests
    /// </summary>
    public bool LogBlockedRequests { get; set; } = true;
}

public class IpBlocklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpBlocklistMiddleware> _logger;
    private readonly IOptionsMonitor<IpBlocklistOptions> _options;

    // Track violations per IP
    private static readonly ConcurrentDictionary<string, (int Count, DateTime FirstViolation)> _violations = new();

    // Auto-blocked IPs with expiration
    private static readonly ConcurrentDictionary<string, DateTime> _autoBlocked = new();

    public IpBlocklistMiddleware(
        RequestDelegate next,
        ILogger<IpBlocklistMiddleware> logger,
        IOptionsMonitor<IpBlocklistOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var realIp = context.GetRealClientIp();
        var ipString = realIp.ToString();
        var country = context.GetClientCountry();
        var options = _options.CurrentValue;

        // Check if IP is in static blocklist
        if (options.BlockedIps.Contains(ipString))
        {
            await RejectRequest(context, ipString, "blocked IP", options.LogBlockedRequests);
            return;
        }

        // Check if country is blocked (exempt search engine bots)
        if (!context.IsSearchEngineBot() &&
            options.BlockedCountries.Contains(country, StringComparer.OrdinalIgnoreCase))
        {
            await RejectRequest(context, ipString, $"blocked country ({country})", options.LogBlockedRequests);
            return;
        }

        // Check if IP is auto-blocked (exempt search engine bots)
        if (!context.IsSearchEngineBot() && _autoBlocked.TryGetValue(ipString, out var blockedUntil))
        {
            if (DateTime.UtcNow < blockedUntil)
            {
                await RejectRequest(context, ipString, "auto-blocked (rate limit violations)", options.LogBlockedRequests);
                return;
            }
            _autoBlocked.TryRemove(ipString, out _);
        }

        await _next(context);
    }

    private async Task RejectRequest(HttpContext context, string ip, string reason, bool log)
    {
        if (log)
        {
            _logger.LogWarning("Blocked request from {Ip}: {Reason}", ip, reason);
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Access denied");
    }

    /// <summary>
    /// Record a rate limit violation for an IP. Called by rate limiter's OnRejected handler.
    /// </summary>
    public static void RecordViolation(string ipAddress, IpBlocklistOptions options, ILogger logger)
    {
        var now = DateTime.UtcNow;

        var violation = _violations.AddOrUpdate(
            ipAddress,
            (1, now),
            (_, existing) =>
            {
                // Reset count if first violation was more than the block duration ago
                if (now - existing.FirstViolation > TimeSpan.FromMinutes(options.AutoBlockDurationMinutes))
                {
                    return (1, now);
                }
                return (existing.Count + 1, existing.FirstViolation);
            });

        if (violation.Count >= options.ViolationsBeforeBlock)
        {
            var blockUntil = now.AddMinutes(options.AutoBlockDurationMinutes);
            _autoBlocked[ipAddress] = blockUntil;
            _violations.TryRemove(ipAddress, out _);

            logger.LogWarning(
                "Auto-blocked IP {Ip} until {BlockUntil} after {Count} rate limit violations",
                ipAddress, blockUntil, violation.Count);
        }
    }

    /// <summary>
    /// Get current stats for monitoring
    /// </summary>
    public static (int TrackedViolations, int AutoBlocked) GetStats()
    {
        // Clean up expired auto-blocks
        var now = DateTime.UtcNow;
        foreach (var kvp in _autoBlocked.Where(x => x.Value < now).ToList())
        {
            _autoBlocked.TryRemove(kvp.Key, out _);
        }

        return (_violations.Count, _autoBlocked.Count);
    }
}

public static class IpBlocklistMiddlewareExtensions
{
    public static IApplicationBuilder UseIpBlocklist(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IpBlocklistMiddleware>();
    }
}
