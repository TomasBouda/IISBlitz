using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TomLabs.IISBlitz.App.ViewModels;

/// <summary>A file the user opened as an extra tab of a site; the tab is remembered per site in the user settings.</summary>
public partial class OpenFileViewModel : ObservableObject
{
    private string _savedContent = string.Empty;

    public OpenFileViewModel(string path)
    {
        Path = path;
        Reload();
    }

    public string Path { get; }

    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>Extension without the dot, used to pick the TextMate grammar.</summary>
    public string Extension => System.IO.Path.GetExtension(Path).TrimStart('.').ToLowerInvariant();

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isMissing;

    [ObservableProperty]
    private string _status = string.Empty;

    partial void OnContentChanged(string value) => IsDirty = value != _savedContent;

    public void Reload()
    {
        try
        {
            if (File.Exists(Path))
            {
                _savedContent = File.ReadAllText(Path);
                IsMissing = false;
                Status = $"{new FileInfo(Path).Length / 1024.0:0.#} KB · {File.GetLastWriteTime(Path):yyyy-MM-dd HH:mm}";
            }
            else
            {
                _savedContent = string.Empty;
                IsMissing = true;
                Status = "file not found";
            }
        }
        catch (Exception ex)
        {
            _savedContent = string.Empty;
            IsMissing = true;
            Status = $"cannot read: {ex.Message}";
        }

        Content = _savedContent;
        IsDirty = false;
    }

    public bool Save()
    {
        try
        {
            File.WriteAllText(Path, Content);
            _savedContent = Content;
            IsDirty = false;
            IsMissing = false;
            Status = $"saved {DateTime.Now:HH:mm:ss}";
            return true;
        }
        catch (Exception ex)
        {
            Status = $"save failed: {ex.Message}";
            return false;
        }
    }
}
