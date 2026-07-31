using System;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Avalonia.Media;

namespace TomLabs.IISBlitz.App.Behaviours;

public class SearchHighlightTransformer : DocumentColorizingTransformer
{
    private static readonly IBrush HighlightBackground = new SolidColorBrush(Color.FromArgb(180, 255, 235, 59));

    public string? SearchTerm { get; set; }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (string.IsNullOrEmpty(SearchTerm)) return;

        var lineText = CurrentContext.Document.GetText(line.Offset, line.Length);
        var idx = 0;
        while ((idx = lineText.IndexOf(SearchTerm, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            ChangeLinePart(
                line.Offset + idx,
                line.Offset + idx + SearchTerm.Length,
                element =>
                {
                    element.TextRunProperties.SetBackgroundBrush(HighlightBackground);
                    element.TextRunProperties.SetForegroundBrush(Brushes.Black);
                });
            idx += SearchTerm.Length;
        }
    }
}
