namespace TomLabs.IISBlitz.App.Models;

/// <summary>One IIS application of a site: the root ("/") or a sub-application, with the pool it runs in.</summary>
public record SiteApplication(string Path, string AppPool, string PhysicalPath)
{
    public bool IsRoot => Path == "/";
}
