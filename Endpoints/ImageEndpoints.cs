using protabula_com.Models;
using protabula_com.Services;

namespace protabula_com.Endpoints;

public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder endpoints, IWebHostEnvironment env)
    {
        // Scene image endpoint - generates color preview images on demand
        // Format: /images/ral-scenes/{slug}-{scene}.jpg (e.g., ral-1000-green-beige-front.jpg)
        endpoints.MapGet("/images/ral-scenes/{filename}.jpg", async (
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
            var colorNumber = RalColor.FromSlug(slug!);
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

        // Development-only endpoints for image generation
        if (env.IsDevelopment())
        {
            // Generate a single color image for testing
            endpoints.MapGet("/dev/generate-image/{slug}", async (string slug, IRalColorLoader colorLoader, IWebHostEnvironment environment) =>
            {
                var fontsDir = Path.Combine(environment.WebRootPath, "fonts");
                var outputDir = Path.Combine(environment.WebRootPath, "images", "ral-colors");

                var colors = await colorLoader.LoadAsync();
                var colorNumber = RalColor.FromSlug(slug);
                var color = colors.FirstOrDefault(c => c.Number == colorNumber);

                if (color == null)
                    return Results.NotFound($"Color {colorNumber} not found");

                var generator = new ColorImageGenerator(fontsDir);
                var outputPath = Path.Combine(outputDir, $"{color.Slug}.jpg");
                generator.GenerateColorImage(color, outputPath);

                return Results.Ok(new { message = $"Generated image for {color.Number}", path = outputPath });
            });

            // Generate all color images
            endpoints.MapGet("/dev/generate-images", async (IRalColorLoader colorLoader, IWebHostEnvironment environment) =>
            {
                var fontsDir = Path.Combine(environment.WebRootPath, "fonts");
                var outputDir = Path.Combine(environment.WebRootPath, "images", "ral-colors");

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

        return endpoints;
    }
}
