namespace TomLabs.IISBlitz.App.Models;

public record FolderPermissionEntry(
    string Identity,
    string Rights,
    string AccessType,
    bool IsInherited);
