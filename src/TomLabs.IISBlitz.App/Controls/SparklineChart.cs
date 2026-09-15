using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TomLabs.IISBlitz.App.Controls;

/// <summary>
/// Minimal response-time sparkline: filled area, 1.5px line, dashed average line and a mono min/avg/max footer.
/// Colours are injected through brushes so the chart follows the active theme.
/// </summary>
public class SparklineChart : Control
{
    public static readonly StyledProperty<IList<double>?> ValuesProperty =
        AvaloniaProperty.Register<SparklineChart, IList<double>?>(nameof(Values));

    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<SparklineChart, IBrush>(nameof(LineBrush), new SolidColorBrush(Color.FromRgb(245, 180, 0)));

    public static readonly StyledProperty<IBrush> FillBrushProperty =
        AvaloniaProperty.Register<SparklineChart, IBrush>(nameof(FillBrush), new SolidColorBrush(Color.FromArgb(26, 245, 180, 0)));

    public static readonly StyledProperty<IBrush> TextBrushProperty =
        AvaloniaProperty.Register<SparklineChart, IBrush>(nameof(TextBrush), new SolidColorBrush(Color.FromRgb(111, 114, 128)));

    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<SparklineChart, IBrush>(nameof(GridBrush), new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)));

    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<SparklineChart, double>(nameof(LineThickness), 1.5);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<SparklineChart, string?>(nameof(Label));

    public static readonly StyledProperty<string?> EmptyTextProperty =
        AvaloniaProperty.Register<SparklineChart, string?>(nameof(EmptyText), "Run a ping to start the chart");

    public IList<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IBrush LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public IBrush FillBrush
    {
        get => GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public IBrush TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public IBrush GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    public double LineThickness
    {
        get => GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? EmptyText
    {
        get => GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    private static readonly Typeface MonoTypeface = new("Cascadia Mono, Consolas, monospace");

    static SparklineChart()
    {
        AffectsRender<SparklineChart>(ValuesProperty, LineBrushProperty, FillBrushProperty, TextBrushProperty,
            GridBrushProperty, LineThicknessProperty, LabelProperty, EmptyTextProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var values = Values;
        const double footer = 16;
        var chartTop = 4.0;
        var chartBottom = bounds.Height - footer;
        var chartLeft = 0.0;
        var chartRight = bounds.Width;

        if (values == null || values.Count < 2)
        {
            if (!string.IsNullOrEmpty(EmptyText))
            {
                var empty = Text(EmptyText, 11, TextBrush);
                context.DrawText(empty, new Point((bounds.Width - empty.Width) / 2, (bounds.Height - empty.Height) / 2));
            }
            return;
        }

        if (chartBottom <= chartTop || chartRight <= chartLeft) return;

        var min = values.Min();
        var max = values.Max();
        var avg = values.Average();
        var range = max - min;
        if (range < 1) range = 1;

        var stepX = (chartRight - chartLeft) / (values.Count - 1);
        var points = new List<Point>(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var x = chartLeft + i * stepX;
            var y = chartBottom - (values[i] - min) / range * (chartBottom - chartTop);
            points.Add(new Point(x, y));
        }

        // Dashed average line
        var avgY = chartBottom - (avg - min) / range * (chartBottom - chartTop);
        var gridPen = new Pen(GridBrush, 1, new DashStyle(new[] { 3.0, 4.0 }, 0));
        context.DrawLine(gridPen, new Point(chartLeft, avgY), new Point(chartRight, avgY));

        // Area fill
        var fill = new StreamGeometry();
        using (var ctx = fill.Open())
        {
            ctx.BeginFigure(new Point(points[0].X, chartBottom), true);
            foreach (var p in points) ctx.LineTo(p);
            ctx.LineTo(new Point(points[^1].X, chartBottom));
            ctx.EndFigure(true);
        }
        context.DrawGeometry(FillBrush, null, fill);

        // Line
        var line = new StreamGeometry();
        using (var ctx = line.Open())
        {
            ctx.BeginFigure(points[0], false);
            for (var i = 1; i < points.Count; i++) ctx.LineTo(points[i]);
            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, new Pen(LineBrush, LineThickness), line);
        context.DrawEllipse(LineBrush, null, points[^1], 2.5, 2.5);

        // Footer: min / avg / max on the left, last value on the right
        var footerY = chartBottom + 3;
        var stats = Text($"min {min:F0} ms   avg {avg:F0} ms   max {max:F0} ms", 10, TextBrush);
        context.DrawText(stats, new Point(chartLeft, footerY));

        var last = Text($"last {values[^1]:F0} ms", 10, LineBrush);
        context.DrawText(last, new Point(chartRight - last.Width, footerY));
    }

    private static FormattedText Text(string text, double size, IBrush brush) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, MonoTypeface, size, brush);
}
