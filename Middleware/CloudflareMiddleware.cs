using System.Net;

namespace protabula_com.Middleware;

/// <summary>
/// Middleware to extract the real client IP from Cloudflare headers.
/// Cloudflare sends the original client IP in CF-Connecting-IP header.
/// </summary>
public class CloudflareMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CloudflareMiddleware> _logger;
    private const string CloudflareConnectingIp = "CF-Connecting-IP";
    private const string CloudflareCountry = "CF-IPCountry";
    private const string RealIpKey = "RealClientIp";
    private const string ClientCountryKey = "ClientCountry";

    public CloudflareMiddleware(RequestDelegate next, ILogger<CloudflareMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var realIp = GetRealClientIp(context);
        var country = context.Request.Headers[CloudflareCountry].FirstOrDefault() ?? "XX";

        context.Items[RealIpKey] = realIp;
        context.Items[ClientCountryKey] = country;

        await _next(context);
    }

    private IPAddress GetRealClientIp(HttpContext context)
    {
        // Priority: CF-Connecting-IP > X-Forwarded-For > RemoteIpAddress
        if (context.Request.Headers.TryGetValue(CloudflareConnectingIp, out var cfIp) &&
            IPAddress.TryParse(cfIp.FirstOrDefault(), out var cloudflareIp))
        {
            return cloudflareIp;
        }

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
        {
            var firstIp = xff.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (IPAddress.TryParse(firstIp, out var forwardedIp))
            {
                return forwardedIp;
            }
        }

        return context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
    }
}

public static class CloudflareMiddlewareExtensions
{
    public static IApplicationBuilder UseCloudflareRealIp(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CloudflareMiddleware>();
    }

    public static IPAddress GetRealClientIp(this HttpContext context)
    {
        return context.Items["RealClientIp"] as IPAddress ?? context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
    }

    public static string GetClientCountry(this HttpContext context)
    {
        return context.Items["ClientCountry"] as string ?? "XX";
    }
}
