using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TomLabs.IISBlitz.App.Controls;

public class SparklineChart : Control
{
    public static readonly StyledProperty<IList<double>?> ValuesProperty =
        AvaloniaProperty.Register<SparklineChart, IList<double>?>(nameof(Values));

    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<SparklineChart, IBrush>(nameof(LineBrush), new SolidColorBrush(Color.FromRgb(79, 195, 247)));

    public static readonly StyledProperty<IBrush> FillBrushProperty =
        AvaloniaProperty.Register<SparklineChart, IBrush>(nameof(FillBrush), new SolidColorBrush(Color.FromArgb(40, 79, 195, 247)));

    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<SparklineChart, double>(nameof(LineThickness), 1.5);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<SparklineChart, string?>(nameof(Label));

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

    static SparklineChart()
    {
        AffectsRender<SparklineChart>(ValuesProperty, LineBrushProperty, FillBrushProperty, LineThicknessProperty, LabelProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var values = Values;
        if (values == null || values.Count < 2) return;

        var bounds = Bounds;
        var padding = 4.0;
        var chartLeft = padding;
        var chartRight = bounds.Width - padding;
        var chartTop = padding + 14;
        var chartBottom = bounds.Height - padding - 16;

        if (chartBottom <= chartTop || chartRight <= chartLeft) return;

        var min = values.Min();
        var max = values.Max();
        var range = max - min;
        if (range < 1) range = 1;

        var stepX = (chartRight - chartLeft) / (values.Count - 1);

        var points = new List<Point>();
        for (int i = 0; i < values.Count; i++)
        {
            var x = chartLeft + i * stepX;
            var y = chartBottom - ((values[i] - min) / range) * (chartBottom - chartTop);
            points.Add(new Point(x, y));
        }

        // Fill area
        var fillGeometry = new StreamGeometry();
        using (var ctx = fillGeometry.Open())
        {
            ctx.BeginFigure(new Point(points[0].X, chartBottom), true);
            foreach (var p in points)
                ctx.LineTo(p);
            ctx.LineTo(new Point(points[^1].X, chartBottom));
            ctx.EndFigure(true);
        }
        context.DrawGeometry(FillBrush, null, fillGeometry);

        // Line
        var pen = new Pen(LineBrush, LineThickness);
        var lineGeometry = new StreamGeometry();
        using (var ctx = lineGeometry.Open())
        {
            ctx.BeginFigure(points[0], false);
            for (int i = 1; i < points.Count; i++)
                ctx.LineTo(points[i]);
            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, pen, lineGeometry);

        // Last point dot
        var lastPoint = points[^1];
        context.DrawEllipse(LineBrush, null, lastPoint, 3, 3);

        // Label
        if (!string.IsNullOrEmpty(Label))
        {
            var labelText = new FormattedText(
                Label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.SemiBold),
                11,
                LineBrush);
            context.DrawText(labelText, new Point(chartLeft, 0));
        }

        // Min/max annotations
        var maxText = new FormattedText(
            $"max: {max:F0}ms",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter"),
            9,
            new SolidColorBrush(Color.FromArgb(150, 200, 200, 200)));
        context.DrawText(maxText, new Point(chartLeft, chartBottom + 2));

        var lastValText = new FormattedText(
            $"last: {values[^1]:F0}ms",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
            9,
            LineBrush);
        context.DrawText(lastValText, new Point(chartRight - lastValText.Width, chartBottom + 2));
    }
}
