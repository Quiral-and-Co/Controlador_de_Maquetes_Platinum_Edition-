using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Platinum.Domain.Layout;

namespace Platinum.Presentation.Avalonia.Views;

/// <summary>2D renderer for the shared millimetre-based TrackEditorEngine document.</summary>
public sealed class TrackCanvas : Control
{
    private const double BaseScale = 0.22;
    public TrackEditorEngine? Engine { get; set; }
    public Guid ActiveLevelId { get; set; }
    public double ZoomFactor { get; set; } = 1d;
    public decimal GridMillimetres { get; set; } = 10m;
    public bool SnapToGrid { get; set; } = true;
    public Guid? SelectedSegmentId { get; set; }
    public Guid? SelectedPieceId { get; set; }
    public (double X, double Y)? PreviewStart { get; set; }
    public (double X, double Y)? PreviewEnd { get; set; }

    public TrackCanvas()
    {
        Width = 1900;
        Height = 1150;
        ClipToBounds = true;
    }

    public (double X, double Y) ToModel(Point viewPoint) =>
        (viewPoint.X / (BaseScale * ZoomFactor) - 80,
         viewPoint.Y / (BaseScale * ZoomFactor) - 80);

    public Point ToView(decimal x, decimal y) =>
        new(((double)x + 80) * BaseScale * ZoomFactor,
            ((double)y + 80) * BaseScale * ZoomFactor);

    public void Refresh()
    {
        Width = 1900 * ZoomFactor;
        Height = 1150 * ZoomFactor;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#121B24")), new Rect(Bounds.Size));
        var scale = BaseScale * ZoomFactor;
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#263746")), 0.7);
        for (var x = 0; x <= 1800; x += 100)
        {
            var px = (x + 80) * scale;
            context.DrawLine(gridPen, new Point(px, 0), new Point(px, 1150 * scale));
        }
        for (var y = 0; y <= 1000; y += 100)
        {
            var py = (y + 80) * scale;
            context.DrawLine(gridPen, new Point(0, py), new Point(1900 * scale, py));
        }

        if (Engine is null) return;
        foreach (var segment in Engine.Segments.Where(x => x.LevelId == ActiveLevelId))
        {
            var selected = segment.Id == SelectedSegmentId;
            var pen = new Pen(new SolidColorBrush(selected ? Color.Parse("#FFD166") : Color.Parse("#72C7E8")), selected ? 5 : 3);
            var points = SegmentPoints(segment);
            for (var i = 1; i < points.Count; i++)
                context.DrawLine(pen, ToView((decimal)points[i - 1].X, (decimal)points[i - 1].Y),
                    ToView((decimal)points[i].X, (decimal)points[i].Y));
            if (segment.Kind == TrackGeometryKind.Turnout)
            {
                var dx = segment.EndX - segment.StartX;
                var dy = segment.EndY - segment.StartY;
                var branchStart = (segment.StartX + dx * 0.48m, segment.StartY + dy * 0.48m);
                var branchEnd = (segment.EndX - dx * 0.42m - dy * 0.28m, segment.EndY - dy * 0.42m + dx * 0.28m);
                context.DrawLine(pen, ToView(branchStart.Item1, branchStart.Item2), ToView(branchEnd.Item1, branchEnd.Item2));
            }
        }

        foreach (var piece in Engine.Pieces.Where(x => x.LevelId == ActiveLevelId))
        {
            var center = ToView(piece.X, piece.Y);
            var selected = piece.Id == SelectedPieceId;
            var fill = new SolidColorBrush(selected ? Color.Parse("#FFD166") : Color.Parse("#E59A45"));
            context.DrawEllipse(fill, new Pen(new SolidColorBrush(Color.Parse("#17212B")), 2), center,
                selected ? 13 : 10, selected ? 13 : 10);
            var angle = (double)piece.RotationDegrees * Math.PI / 180d;
            var end = new Point(center.X + Math.Cos(angle) * 24 * ZoomFactor,
                center.Y + Math.Sin(angle) * 24 * ZoomFactor);
            context.DrawLine(new Pen(fill, 3), center, end);
        }

        if (PreviewStart.HasValue && PreviewEnd.HasValue)
        {
            var a = new Point((PreviewStart.Value.X + 80) * scale, (PreviewStart.Value.Y + 80) * scale);
            var b = new Point((PreviewEnd.Value.X + 80) * scale, (PreviewEnd.Value.Y + 80) * scale);
            context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#B5E48C")), 2, DashStyle.Dash), a, b);
        }
    }

    private static IReadOnlyList<(double X, double Y)> SegmentPoints(TrackGeometrySegment s)
    {
        if (s.Kind != TrackGeometryKind.Curve)
            return [(double)s.StartX is var x ? (x, (double)s.StartY) : default, ((double)s.EndX, (double)s.EndY)];
        var points = new List<(double X, double Y)>();
        for (var i = 0; i <= 32; i++)
        {
            var t = i / 32d;
            var u = 1d - t;
            points.Add((u * u * (double)s.StartX + 2d * u * t * (double)s.ControlX + t * t * (double)s.EndX,
                u * u * (double)s.StartY + 2d * u * t * (double)s.ControlY + t * t * (double)s.EndY));
        }
        return points;
    }
}