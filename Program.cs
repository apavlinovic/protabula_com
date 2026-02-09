using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using protabula_com.Endpoints;
using protabula_com.Localization;
using protabula_com.Middleware;
using protabula_com.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddLocalization(options => options.ResourcesPath = "ResourcesJson");
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
builder.Services.AddSingleton<IHtmlLocalizerFactory, JsonHtmlLocalizerFactory>();
builder.Services.AddSingleton<IRootColorClassifier, RootColorClassifier>();
builder.Services.AddSingleton<IRalColorLoader, RalColorLoader>();
builder.Services.AddSingleton<ISimilarColorFinder, SimilarColorFinder>();
builder.Services.AddSingleton<ISitemapGenerator, SitemapGenerator>();
builder.Services.AddSingleton<IColorImageService, ColorImageService>();

// Configure IP blocklist options
builder.Services.Configure<IpBlocklistOptions>(
    builder.Configuration.GetSection(IpBlocklistOptions.SectionName));

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global rate limit: 200 requests per minute per IP (bots exempt)
    options.AddPolicy("global", context =>
    {
        if (context.IsSearchEngineBot())
            return RateLimitPartition.GetNoLimiter(string.Empty);

        var ip = context.GetRealClientIp().ToString();
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 200,
            QueueLimit = 10
        });
    });

    // Stricter limit for color detail pages: 60 requests per minute per IP (bots exempt)
    options.AddPolicy("color-pages", context =>
    {
        if (context.IsSearchEngineBot())
            return RateLimitPartition.GetNoLimiter(string.Empty);

        var ip = context.GetRealClientIp().ToString();
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 60,
            QueueLimit = 5
        });
    });

    // Strict limit for API endpoints: 30 requests per minute per IP (bots exempt)
    options.AddPolicy("api", context =>
    {
        if (context.IsSearchEngineBot())
            return RateLimitPartition.GetNoLimiter(string.Empty);

        var ip = context.GetRealClientIp().ToString();
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 30,
            QueueLimit = 5
        });
    });

    // Handle rejected requests - record violations for auto-blocking
    options.OnRejected = async (context, cancellationToken) =>
    {
        var ip = context.HttpContext.GetRealClientIp().ToString();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var blocklistOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<IpBlocklistOptions>>().CurrentValue;

        IpBlocklistMiddleware.RecordViolation(ip, blocklistOptions, logger);

        logger.LogWarning("Rate limit exceeded for IP {Ip} on {Path}",
            ip, context.HttpContext.Request.Path);

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain";
        await context.HttpContext.Response.WriteAsync("Too many requests. Please slow down.", cancellationToken);
    };
});

// Configure forwarded headers for reverse proxy (nginx, Cloudflare, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorPages(options =>
{
    const string cultureRouteConstraint = "{culture:regex(^en$|^de$)}";
    options.Conventions.AddFolderRouteModelConvention("/", model =>
    {
        foreach (var selector in model.Selectors)
        {
            var template = selector.AttributeRouteModel?.Template ?? string.Empty;
            selector.AttributeRouteModel ??= new AttributeRouteModel();
            selector.AttributeRouteModel.Template = string.IsNullOrEmpty(template)
                ? cultureRouteConstraint
                : $"{cultureRouteConstraint}/{template}";
        }
    });
})
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/en/Error");
    app.UseStatusCodePagesWithReExecute("/en/Error", "?statusCode={0}");
    app.UseHsts();
}

app.UseForwardedHeaders();

// Bot protection middleware (must be early in pipeline)
app.UseCloudflareRealIp();  // Extract real IP from Cloudflare headers
app.UseIpBlocklist();        // Block banned IPs and countries
app.UseRateLimiter();        // Apply rate limiting

app.UseHttpsRedirection();

// Only enable response compression in non-development environments
// (conflicts with dotnet watch browser refresh script injection)
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

// Security headers
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "1; mode=block";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

    await next();
});

var supportedCultures = new[] { "en", "de" }
    .Select(c => new CultureInfo(c))
    .ToList();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders = new[]
    {
        new RouteDataRequestCultureProvider { RouteDataStringKey = "culture", UIRouteDataStringKey = "culture" }
    }
};

app.UseRouting();

app.UseRequestLocalization(localizationOptions);

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets()
   .RequireRateLimiting("global");

// Redirect root to English
app.MapGet("/", () => Results.Redirect("/en"));

// Map API endpoints with stricter rate limits via route groups
var apiGroup = app.MapGroup("/api").RequireRateLimiting("api");
apiGroup.MapGet("/ral/match", ColorEndpoints.HandleRalMatch);
apiGroup.MapGet("/colors/search", ColorEndpoints.HandleColorSearch);

// Map other endpoints (use global rate limit)
app.MapImageEndpoints(app.Environment);
app.MapSeoEndpoints();

app.Run();
