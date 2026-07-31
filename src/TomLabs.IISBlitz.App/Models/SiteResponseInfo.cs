using System.Collections.Generic;

namespace TomLabs.IISBlitz.App.Models;

public record HttpHeaderItem(string Name, string Value);

public class SiteResponseInfo
{
    public int StatusCode { get; init; }
    public string StatusDescription { get; init; } = string.Empty;
    public long ResponseTimeMs { get; init; }
    public long ContentLength { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string Server { get; init; } = string.Empty;
    public string PoweredBy { get; init; } = string.Empty;
    public string PageTitle { get; init; } = string.Empty;
    public string MetaDescription { get; init; } = string.Empty;
    public string MetaGenerator { get; init; } = string.Empty;
    public List<HttpHeaderItem> Headers { get; init; } = new();
    public string RawHtml { get; init; } = string.Empty;
}
