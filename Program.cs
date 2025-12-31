using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using protabula_com.Localization;
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
app.UseHttpsRedirection();
app.UseResponseCompression();

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
   .WithStaticAssets();

// Redirect root to English
app.MapGet("/", () => Results.Redirect("/en"));

// Sitemap endpoint
app.MapGet("/sitemap.xml", async (HttpContext context, ISitemapGenerator sitemapGenerator) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var sitemap = await sitemapGenerator.GenerateAsync(baseUrl, context.RequestAborted);
    return Results.Content(sitemap, "application/xml");
});

// Robots.txt endpoint
app.MapGet("/robots.txt", (HttpContext context) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var robotsTxt = $"""
        User-agent: *
        Allow: /

        Sitemap: {baseUrl}/sitemap.xml
        """;
    return Results.Content(robotsTxt, "text/plain");
});

// Color search API endpoint - returns results grouped by category
app.MapGet("/api/colors/search", async (string? q, string? culture, IRalColorLoader colorLoader) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
    {
        return Results.Ok(new { categories = Array.Empty<object>() });
    }

    var lang = culture == "de" ? "de" : "en";
    var colors = await colorLoader.LoadAsync();
    var query = q.Trim();

    var matchingColors = colors
        .Where(c =>
            c.Number.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.GetLocalizedName(lang).Contains(query, StringComparison.OrdinalIgnoreCase));

    var categoryNames = new Dictionary<protabula_com.Models.RalCategory, (string key, string en, string de)>
    {
        { protabula_com.Models.RalCategory.Classic, ("classic", "RAL Classic", "RAL Classic") },
        { protabula_com.Models.RalCategory.DesignPlus, ("design-plus", "RAL Design System Plus", "RAL Design System Plus") },
        { protabula_com.Models.RalCategory.Effect, ("effect", "RAL Effect", "RAL Effect") }
    };

    var categories = matchingColors
        .GroupBy(c => c.Category)
        .OrderBy(g => g.Key)
        .Select(g => new
        {
            key = categoryNames[g.Key].key,
            name = lang == "de" ? categoryNames[g.Key].de : categoryNames[g.Key].en,
            colors = g.Take(10).Select(c => new
            {
                number = c.Number,
                name = c.GetLocalizedName(lang),
                hex = c.Hex,
                slug = c.Slug,
                needsDarkText = c.NeedsDarkText
            }).ToArray()
        })
        .Where(c => c.colors.Length > 0)
        .ToArray();

    return Results.Ok(new { categories });
});

// Scene image endpoint - generates color preview images on demand
// Format: /images/ral-scenes/{slug}-{scene}.jpg (e.g., ral-1000-green-beige-front.jpg)
app.MapGet("/images/ral-scenes/{filename}.jpg", async (
    string filename,
    IRalColorLoader colorLoader,
    IColorImageService imageService,
    HttpContext context) =>
{
    // Parse filename to extract slug and scene using optimized parser
    if (!imageService.TryParseFilename(filename, out var slug, out var scene))
    {
        return Results.NotFound();
    }

    // Parse slug to get color number (slug is guaranteed non-null after TryParseFilename succeeds)
    var colorNumber = protabula_com.Models.RalColor.FromSlug(slug!);
    var colors = await colorLoader.LoadAsync();
    var color = colors.FirstOrDefault(c => c.Number == colorNumber);

    if (color == null)
    {
        return Results.NotFound();
    }

    try
    {
        var imagePath = await imageService.GetOrGenerateSceneImageAsync(color.Slug, color.Hex, scene!);

        // Set cache headers (cache for 1 year since images don't change)
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        // Set Content-Disposition for better download filename
        var downloadFilename = $"{color.Slug}-{scene}.jpg";
        context.Response.Headers.ContentDisposition = $"inline; filename=\"{downloadFilename}\"";

        return Results.File(imagePath, "image/jpeg");
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
});

// Development-only: Generate color preview images
if (app.Environment.IsDevelopment())
{
    // Generate a single color image for testing
    app.MapGet("/dev/generate-image/{slug}", async (string slug, IRalColorLoader colorLoader, IWebHostEnvironment env) =>
    {
        var fontsDir = Path.Combine(env.WebRootPath, "fonts");
        var outputDir = Path.Combine(env.WebRootPath, "images", "ral-colors");

        var colors = await colorLoader.LoadAsync();
        var colorNumber = protabula_com.Models.RalColor.FromSlug(slug);
        var color = colors.FirstOrDefault(c => c.Number == colorNumber);

        if (color == null)
            return Results.NotFound($"Color {colorNumber} not found");

        var generator = new ColorImageGenerator(fontsDir);
        var outputPath = Path.Combine(outputDir, $"{color.Slug}.jpg");
        generator.GenerateColorImage(color, outputPath);

        return Results.Ok(new { message = $"Generated image for {color.Number}", path = outputPath });
    });

    // Generate all color images
    app.MapGet("/dev/generate-images", async (IRalColorLoader colorLoader, IWebHostEnvironment env) =>
    {
        var fontsDir = Path.Combine(env.WebRootPath, "fonts");
        var outputDir = Path.Combine(env.WebRootPath, "images", "ral-colors");

        var generator = new ColorImageGenerator(fontsDir);
        var colors = await colorLoader.LoadAsync();

        var generated = 0;
        await generator.GenerateAllColorImagesAsync(
            colors,
            outputDir,
            "en",
            new Progress<(int current, int total, string colorNumber)>(p =>
            {
                generated = p.current;
            }));

        return Results.Ok(new { message = $"Generated {generated} color images", outputDirectory = outputDir });
    });
}

app.Run();
