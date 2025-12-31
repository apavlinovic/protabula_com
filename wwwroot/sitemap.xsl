<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="2.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:sitemap="http://www.sitemaps.org/schemas/sitemap/0.9"
    xmlns:image="http://www.google.com/schemas/sitemap-image/1.1"
    xmlns:xhtml="http://www.w3.org/1999/xhtml">
  <xsl:output method="html" encoding="UTF-8" indent="yes"/>

  <xsl:template match="/">
    <html>
      <head>
        <title>Sitemap</title>
        <style>
          * { box-sizing: border-box; }
          body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
            font-size: 14px;
            color: #333;
            background: #f5f5f5;
            margin: 0;
            padding: 20px;
          }
          .container {
            max-width: 1200px;
            margin: 0 auto;
            background: #fff;
            border-radius: 8px;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
            overflow: hidden;
          }
          header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #fff;
            padding: 24px 32px;
          }
          header h1 {
            margin: 0 0 8px 0;
            font-size: 24px;
            font-weight: 600;
          }
          header p {
            margin: 0;
            opacity: 0.9;
            font-size: 14px;
          }
          .stats {
            display: flex;
            gap: 24px;
            margin-top: 16px;
          }
          .stat {
            background: rgba(255,255,255,0.15);
            padding: 8px 16px;
            border-radius: 4px;
          }
          .stat-value {
            font-size: 20px;
            font-weight: 600;
          }
          .stat-label {
            font-size: 12px;
            opacity: 0.8;
          }
          table {
            width: 100%;
            border-collapse: collapse;
          }
          th {
            background: #f8f9fa;
            padding: 12px 16px;
            text-align: left;
            font-weight: 600;
            font-size: 12px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            color: #666;
            border-bottom: 2px solid #e9ecef;
          }
          td {
            padding: 12px 16px;
            border-bottom: 1px solid #e9ecef;
            vertical-align: top;
          }
          tr:hover td {
            background: #f8f9fa;
          }
          a {
            color: #667eea;
            text-decoration: none;
            word-break: break-all;
          }
          a:hover {
            text-decoration: underline;
          }
          .priority {
            display: inline-block;
            padding: 2px 8px;
            border-radius: 12px;
            font-size: 12px;
            font-weight: 500;
          }
          .priority-high { background: #d4edda; color: #155724; }
          .priority-medium { background: #fff3cd; color: #856404; }
          .priority-low { background: #e9ecef; color: #495057; }
          .images {
            font-size: 12px;
            color: #666;
            margin-top: 4px;
          }
          .lang-tags {
            display: flex;
            gap: 4px;
            flex-wrap: wrap;
            margin-top: 4px;
          }
          .lang-tag {
            background: #e9ecef;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 11px;
            color: #495057;
          }
        </style>
      </head>
      <body>
        <div class="container">
          <header>
            <h1>XML Sitemap</h1>
            <p>This sitemap contains all URLs for search engine indexing.</p>
            <div class="stats">
              <div class="stat">
                <div class="stat-value"><xsl:value-of select="count(sitemap:urlset/sitemap:url)"/></div>
                <div class="stat-label">Total URLs</div>
              </div>
              <div class="stat">
                <div class="stat-value"><xsl:value-of select="count(sitemap:urlset/sitemap:url/image:image)"/></div>
                <div class="stat-label">Images</div>
              </div>
            </div>
          </header>
          <table>
            <thead>
              <tr>
                <th>URL</th>
                <th>Priority</th>
                <th>Change Freq</th>
              </tr>
            </thead>
            <tbody>
              <xsl:for-each select="sitemap:urlset/sitemap:url">
                <tr>
                  <td>
                    <a href="{sitemap:loc}"><xsl:value-of select="sitemap:loc"/></a>
                    <xsl:if test="image:image">
                      <div class="images">
                        <xsl:value-of select="count(image:image)"/> image(s)
                      </div>
                    </xsl:if>
                    <xsl:if test="xhtml:link[@rel='alternate']">
                      <div class="lang-tags">
                        <xsl:for-each select="xhtml:link[@rel='alternate']">
                          <span class="lang-tag"><xsl:value-of select="@hreflang"/></span>
                        </xsl:for-each>
                      </div>
                    </xsl:if>
                  </td>
                  <td>
                    <xsl:variable name="priority" select="sitemap:priority"/>
                    <span>
                      <xsl:attribute name="class">
                        <xsl:text>priority </xsl:text>
                        <xsl:choose>
                          <xsl:when test="$priority >= 0.8">priority-high</xsl:when>
                          <xsl:when test="$priority >= 0.5">priority-medium</xsl:when>
                          <xsl:otherwise>priority-low</xsl:otherwise>
                        </xsl:choose>
                      </xsl:attribute>
                      <xsl:value-of select="sitemap:priority"/>
                    </span>
                  </td>
                  <td><xsl:value-of select="sitemap:changefreq"/></td>
                </tr>
              </xsl:for-each>
            </tbody>
          </table>
        </div>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
