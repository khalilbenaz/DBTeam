using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace DBTeam.Modules.QueryEditor.Intellisense;

public sealed class ErrorRenderer : IBackgroundRenderer
{
    public List<(int start, int end, string message)> Errors { get; } = new();

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (Errors.Count == 0) return;
        textView.EnsureVisualLines();
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)), 1);
        pen.Freeze();
        foreach (var (start, end, _) in Errors)
        {
            if (start < 0 || end <= start) continue;
            var segment = new ICSharpCode.AvalonEdit.Document.TextSegment { StartOffset = start, EndOffset = end };
            foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                // squiggle underline
                double y = r.Bottom - 1;
                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(new Point(r.Left, y), false, false);
                    bool up = true;
                    for (double x = r.Left; x < r.Right; x += 3)
                    {
                        ctx.LineTo(new Point(x + 3, up ? y - 2 : y + 1), true, false);
                        up = !up;
                    }
                }
                geo.Freeze();
                drawingContext.DrawGeometry(null, pen, geo);
            }
        }
    }
}
