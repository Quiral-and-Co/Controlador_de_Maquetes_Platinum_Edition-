using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Platinum.Domain.Layout;
using Platinum.Domain.Catalogue;

namespace Platinum.Presentation.Avalonia.Views;

/// <summary>2D renderer for the shared millimetre-based TrackEditorEngine document.</summary>
public sealed class TrackCanvas : Control
{
    private const double BaseScale = 0.22;
    public TrackEditorEngine? Engine { get; set; }
    public IReadOnlyList<CatalogueItem> CatalogueItems { get; set; } = [];
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
        foreach (var connection in Engine.Connections)
        {
            var fromPiece = Engine.Pieces.FirstOrDefault(p => p.Id == connection.FromPieceId && p.LevelId == ActiveLevelId);
            var toPiece = Engine.Pieces.FirstOrDefault(p => p.Id == connection.ToPieceId && p.LevelId == ActiveLevelId);
            var a = Engine.GetWorldPort(connection.FromPieceId, connection.FromPort);
            var b = Engine.GetWorldPort(connection.ToPieceId, connection.ToPort);
            if (fromPiece is null || toPiece is null || !a.HasValue || !b.HasValue) continue;
            var pen = new Pen(new SolidColorBrush(connection.PhysicallyVerified ? Color.Parse("#72D69A") : Color.Parse("#F4A261")),
                2.2, connection.PhysicallyVerified ? DashStyle.Solid : DashStyle.Dash);
            context.DrawLine(pen, ToView(a.Value.X, a.Value.Y), ToView(b.Value.X, b.Value.Y));
        }

        foreach (var segment in Engine.Segments.Where(x => x.LevelId == ActiveLevelId))
        {
            var selected = segment.Id == SelectedSegmentId;
            var pen = new Pen(new SolidColorBrush(selected ? Color.Parse("#FFD166") : Color.Parse("#DDE6EC")), selected ? 7 : 5);
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
            DrawCataloguePiece(context, piece);

        if (PreviewStart.HasValue && PreviewEnd.HasValue)
        {
            var a = new Point((PreviewStart.Value.X + 80) * scale, (PreviewStart.Value.Y + 80) * scale);
            var b = new Point((PreviewEnd.Value.X + 80) * scale, (PreviewEnd.Value.Y + 80) * scale);
            context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#B5E48C")), 2, DashStyle.Dash), a, b);
        }
    }

    private void DrawCataloguePiece(DrawingContext context, TrackPiece piece)
    {
        var item = CatalogueItems.FirstOrDefault(x => x.Ref.Equals(piece.CatalogueCode, StringComparison.OrdinalIgnoreCase));
        var selected = piece.Id == SelectedPieceId;
        var color = item?.GeometryStatus switch
        {
            "confirmed" => Color.Parse("#53BED2"),
            "partial" => Color.Parse("#F0AE52"),
            _ => Color.Parse("#A5AEB8")
        };
        var pen = new Pen(new SolidColorBrush(selected ? Color.Parse("#FFD166") : color), selected ? 7 : 5);
        if (item is not null && item.TrackPaths.Count > 0)
        {
            var ports = item.Ports.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var route in item.TrackPaths)
            {
                if (!ports.TryGetValue(route.FromPort, out var from) || !ports.TryGetValue(route.ToPort, out var to)) continue;
                var points = RoutePoints(piece, route, from, to);
                for (var i = 1; i < points.Count; i++)
                    context.DrawLine(pen, ToView(points[i - 1].X, points[i - 1].Y), ToView(points[i].X, points[i].Y));
            }
        }
        else
        {
            var center = ToView(piece.X, piece.Y);
            context.DrawEllipse(new SolidColorBrush(color), pen, center, 5 * ZoomFactor, 5 * ZoomFactor);
        }

        if (item is null) return;
        foreach (var port in item.Ports)
        {
            var world = TrackEditorEngine.TransformPort(piece, port.X, port.Y);
            var point = ToView(world.X, world.Y);
            var marker = port.Accuracy == "estimated" ? Color.Parse("#F4A261") : Color.Parse("#7AE582");
            context.DrawEllipse(new SolidColorBrush(marker), new Pen(new SolidColorBrush(Color.Parse("#17212B")), 1),
                point, 4.5 * ZoomFactor, 4.5 * ZoomFactor);
        }
    }

    private static IReadOnlyList<(decimal X, decimal Y)> RoutePoints(TrackPiece piece,
        CatalogueTrackPath route, CataloguePort from, CataloguePort to)
    {
        var points = new List<(decimal X, decimal Y)>();
        var start = TrackEditorEngine.TransformPort(piece, from.X, from.Y);
        var end = TrackEditorEngine.TransformPort(piece, to.X, to.Y);
        if (route.Kind.Equals("quadratic", StringComparison.OrdinalIgnoreCase))
        {
            var control = TrackEditorEngine.TransformPort(piece, route.ControlX, route.ControlY);
            for (var i = 0; i <= 24; i++)
            {
                var t = i / 24m;
                var u = 1m - t;
                points.Add((u * u * start.X + 2m * u * t * control.X + t * t * end.X,
                    u * u * start.Y + 2m * u * t * control.Y + t * t * end.Y));
            }
        }
        else points.AddRange([start, end]);
        return points;
    }

    private static IReadOnlyList<(double X, double Y)> SegmentPoints(TrackGeometrySegment s)
    {
        if (s.Kind != TrackGeometryKind.Curve)
            return [((double)s.StartX, (double)s.StartY), ((double)s.EndX, (double)s.EndY)];
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