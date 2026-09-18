using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TomLabs.IISBlitz.App.ViewModels;

namespace TomLabs.IISBlitz.App.Views;

public partial class FileEditorView : UserControl
{
    private TextMate.Installation? _textMate;

    public FileEditorView()
    {
        InitializeComponent();
        SaveButton.Click += (_, _) => (DataContext as OpenFileViewModel)?.Save();
        ReloadButton.Click += (_, _) => (DataContext as OpenFileViewModel)?.Reload();
        DataContextChanged += (_, _) => SetupHighlighting();
        ActualThemeVariantChanged += (_, _) => ApplyTheme();
        Loaded += (_, _) => ApplyTheme();
    }

    /// <summary>Picks the TextMate grammar by file extension; unknown extensions stay plain text.</summary>
    private void SetupHighlighting()
    {
        if (DataContext is not OpenFileViewModel file) return;
        var dark = ActualThemeVariant == ThemeVariant.Dark;
        var registry = new RegistryOptions(dark ? ThemeName.DarkPlus : ThemeName.LightPlus);
        _textMate ??= Editor.InstallTextMate(registry);

        var language = registry.GetLanguageByExtension("." + file.Extension);
        if (language != null)
            _textMate.SetGrammar(registry.GetScopeByLanguageId(language.Id));
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var dark = ActualThemeVariant == ThemeVariant.Dark;
        var registry = new RegistryOptions(dark ? ThemeName.DarkPlus : ThemeName.LightPlus);
        try { _textMate?.SetTheme(registry.LoadTheme(dark ? ThemeName.DarkPlus : ThemeName.LightPlus)); }
        catch (Exception ex) { Console.Error.WriteLine($"Editor theme: {ex.Message}"); }

        if (this.TryFindResource("BgEditorBrush", ActualThemeVariant, out var bg) && bg is IBrush bgBrush) Editor.Background = bgBrush;
        if (this.TryFindResource("TextBrush", ActualThemeVariant, out var fg) && fg is IBrush fgBrush) Editor.Foreground = fgBrush;
        if (this.TryFindResource("TextFaintBrush", ActualThemeVariant, out var ln) && ln is IBrush lnBrush) Editor.LineNumbersForeground = lnBrush;
    }
}
