using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using protabula_com.Endpoints;
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
   .WithStaticAssets();

// Redirect root to English
app.MapGet("/", () => Results.Redirect("/en"));

// Map API and utility endpoints
app.MapColorEndpoints();
app.MapImageEndpoints(app.Environment);
app.MapSeoEndpoints();

app.Run();
