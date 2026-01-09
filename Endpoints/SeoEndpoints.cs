using protabula_com.Services;

namespace protabula_com.Endpoints;

public static class SeoEndpoints
{
    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Sitemap.xml endpoint
        endpoints.MapGet("/sitemap.xml", async (HttpContext context, ISitemapGenerator sitemapGenerator) =>
        {
            var request = context.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var sitemap = await sitemapGenerator.GenerateAsync(baseUrl, context.RequestAborted);

            context.Response.ContentType = "application/xml; charset=utf-8";
            await context.Response.WriteAsync(sitemap);
        });

        // Robots.txt endpoint
        endpoints.MapGet("/robots.txt", async (HttpContext context) =>
        {
            var request = context.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            var robots = $"""
                User-agent: *
                Allow: /

                Sitemap: {baseUrl}/sitemap.xml
                """;

            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(robots);
        });

        return endpoints;
    }
}
