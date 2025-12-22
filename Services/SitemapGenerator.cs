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
    private static readonly string[] StaticPages = ["", "ral-colors", "ral-colors/converter", "ral-colors/compare"];
    private static readonly string[] CategoryPages = ["ral-colors/classic", "ral-colors/design-plus", "ral-colors/effect"];

    private readonly IRalColorLoader _colorLoader;

    public SitemapGenerator(IRalColorLoader colorLoader)
    {
        _colorLoader = colorLoader;
    }

    public async Task<string> GenerateAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var colors = await _colorLoader.LoadAsync(cancellationToken);
        var sb = new StringBuilder();

        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
        writer.WriteAttributeString("xmlns", "xhtml", null, "http://www.w3.org/1999/xhtml");

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

        // Color detail pages
        foreach (var color in colors)
        {
            var path = $"ral-colors/{color.Slug}";
            WriteUrlWithAlternates(writer, baseUrl, path);
        }

        writer.WriteEndElement(); // urlset
        writer.WriteEndDocument();
        writer.Flush();

        return sb.ToString();
    }

    private static void WriteUrlWithAlternates(XmlWriter writer, string baseUrl, string path)
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

            writer.WriteEndElement(); // url
        }
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
