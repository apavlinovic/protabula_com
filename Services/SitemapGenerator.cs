using System.Text;
using System.Xml;

namespace protabula_com.Services;

public interface ISitemapGenerator
{
    Task<string> GenerateAsync(string baseUrl, CancellationToken cancellationToken = default);
}

public sealed class SitemapGenerator : ISitemapGenerator
{
    private static readonly string[] SupportedCultures = ["en", "de"];
    private static readonly string[] StaticPages = ["", "ral-colors", "ral-colors/converter", "ral-colors/compare", "privacy"];
    private static readonly string[] CategoryPages = ["ral-colors/classic", "ral-colors/design-plus", "ral-colors/effect"];

    private readonly IRalColorLoader _colorLoader;
    private readonly IColorImageService _colorImageService;

    public SitemapGenerator(IRalColorLoader colorLoader, IColorImageService colorImageService)
    {
        _colorLoader = colorLoader;
        _colorImageService = colorImageService;
    }

    public async Task<string> GenerateAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var colors = await _colorLoader.LoadAsync(cancellationToken);

        using var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false), // UTF-8 without BOM
            OmitXmlDeclaration = false,
            Async = true
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
        writer.WriteAttributeString("xmlns", "xhtml", null, "http://www.w3.org/1999/xhtml");
        writer.WriteAttributeString("xmlns", "image", null, "http://www.google.com/schemas/sitemap-image/1.1");

        // Static pages
        foreach (var page in StaticPages)
        {
            WriteUrlWithAlternates(writer, baseUrl, page);
        }

        // Category pages
        foreach (var page in CategoryPages)
        {
            WriteUrlWithAlternates(writer, baseUrl, page);
        }

        // Color detail pages with images
        var validScenes = _colorImageService.GetValidScenes();
        foreach (var color in colors)
        {
            var path = $"ral-colors/{color.Slug}";
            var colorTitle = string.IsNullOrEmpty(color.Name)
                ? $"RAL {color.Number}"
                : $"RAL {color.Number} {color.Name}";

            // Build list of images: main swatch + scene images
            var images = new List<(string Url, string Title, string Caption)>
            {
                ($"{baseUrl}/images/ral-colors/{color.Slug}.jpg", colorTitle, $"RAL {color.Number} color swatch - {color.Hex} hex code")
            };

            // Add scene images
            foreach (var scene in validScenes)
            {
                var sceneTitle = $"{colorTitle} {FormatSceneName(scene)}";
                images.Add(($"{baseUrl}/images/ral-scenes/{color.Slug}-{scene}.jpg", sceneTitle, sceneTitle));
            }

            WriteUrlWithAlternates(writer, baseUrl, path, images);
        }

        writer.WriteEndElement(); // urlset
        writer.WriteEndDocument();
        await writer.FlushAsync();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteUrlWithAlternates(
        XmlWriter writer,
        string baseUrl,
        string path,
        IList<(string Url, string Title, string Caption)>? images = null)
    {
        foreach (var culture in SupportedCultures)
        {
            var url = string.IsNullOrEmpty(path)
                ? $"{baseUrl}/{culture}"
                : $"{baseUrl}/{culture}/{path}";

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", url);
            writer.WriteElementString("changefreq", "weekly");
            writer.WriteElementString("priority", GetPriority(path));

            // Add hreflang alternates
            foreach (var altCulture in SupportedCultures)
            {
                var altUrl = string.IsNullOrEmpty(path)
                    ? $"{baseUrl}/{altCulture}"
                    : $"{baseUrl}/{altCulture}/{path}";

                writer.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
                writer.WriteAttributeString("rel", "alternate");
                writer.WriteAttributeString("hreflang", altCulture);
                writer.WriteAttributeString("href", altUrl);
                writer.WriteEndElement();
            }

            // x-default points to English
            var defaultUrl = string.IsNullOrEmpty(path)
                ? $"{baseUrl}/en"
                : $"{baseUrl}/en/{path}";
            writer.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
            writer.WriteAttributeString("rel", "alternate");
            writer.WriteAttributeString("hreflang", "x-default");
            writer.WriteAttributeString("href", defaultUrl);
            writer.WriteEndElement();

            // Add images if provided
            if (images != null)
            {
                foreach (var (imageUrl, imageTitle, imageCaption) in images)
                {
                    writer.WriteStartElement("image", "image", "http://www.google.com/schemas/sitemap-image/1.1");
                    writer.WriteElementString("image", "loc", "http://www.google.com/schemas/sitemap-image/1.1", imageUrl);
                    writer.WriteElementString("image", "title", "http://www.google.com/schemas/sitemap-image/1.1", imageTitle);
                    writer.WriteElementString("image", "caption", "http://www.google.com/schemas/sitemap-image/1.1", imageCaption);
                    writer.WriteEndElement(); // image:image
                }
            }

            writer.WriteEndElement(); // url
        }
    }

    private static string FormatSceneName(string scene)
    {
        // Convert "front-door" to "Front Door"
        return string.Join(' ', scene.Split('-').Select(word =>
            char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private static string GetPriority(string path)
    {
        return path switch
        {
            "" => "1.0",
            "ral-colors" => "0.9",
            _ when path.StartsWith("ral-colors/classic") ||
                   path.StartsWith("ral-colors/design-plus") ||
                   path.StartsWith("ral-colors/effect") => "0.8",
            _ when path.StartsWith("ral-colors/converter") ||
                   path.StartsWith("ral-colors/compare") => "0.7",
            _ => "0.6"
        };
    }
}
