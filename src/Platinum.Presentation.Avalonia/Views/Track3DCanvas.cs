using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Platinum.Domain.Layout;
using Platinum.Domain.Catalogue;

namespace Platinum.Presentation.Avalonia.Views;

/// <summary>Interactive 3D projection preview over the same domain centre-lines, ports and elevations.</summary>
public sealed class Track3DCanvas : Control
{
    private double _yaw = -42;
    private double _pitch = 42;
    private double _zoom = 0.34;
    private Point _lastPointer;
    private bool _orbiting;

    public TrackEditorEngine? Engine { get; set; }
    public IReadOnlyList<CatalogueItem> CatalogueItems { get; set; } = [];
    public IReadOnlyList<TrackLevel> Levels { get; set; } = [new TrackLevel(Guid.Empty, "Nivell base", 0m)];
    public Guid ActiveLevelId { get; set; }
    public bool ShowAllLevels { get; set; } = true;

    public Track3DCanvas()
    {
        MinHeight = 420;
        ClipToBounds = true;
        Focusable = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#101923")), new Rect(Bounds.Size));
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2 + 20);
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#273846")), 0.8);
        for (var i = -10; i <= 10; i++)
        {
            context.DrawLine(gridPen, Project(i * 100, -1000, -8, center), Project(i * 100, 1000, -8, center));
            context.DrawLine(gridPen, Project(-1000, i * 100, -8, center), Project(1000, i * 100, -8, center));
        }

        if (Engine is null) return;
        foreach (var connection in Engine.Connections)
        {
            var a = Engine.GetWorldPort(connection.FromPieceId, connection.FromPort);
            var b = Engine.GetWorldPort(connection.ToPieceId, connection.ToPort);
            if (!a.HasValue || !b.HasValue) continue;
            var from = Engine.Pieces.FirstOrDefault(p => p.Id == connection.FromPieceId);
            var to = Engine.Pieces.FirstOrDefault(p => p.Id == connection.ToPieceId);
            if (from is null || to is null) continue;
            var fromLevel = Levels.FirstOrDefault(l => l.Id == from.LevelId);
            var toLevel = Levels.FirstOrDefault(l => l.Id == to.LevelId);
            var pen = new Pen(new SolidColorBrush(connection.PhysicallyVerified ? Color.Parse("#72D69A") : Color.Parse("#F4A261")), 2.4);
            context.DrawLine(pen,
                Project((double)a.Value.X, -(double)a.Value.Y, (double)(fromLevel?.ElevationMillimetres ?? 0m) + 8, center),
                Project((double)b.Value.X, -(double)b.Value.Y, (double)(toLevel?.ElevationMillimetres ?? 0m) + 8, center));
        }

        foreach (var level in Levels.Where(l => ShowAllLevels || l.Id == ActiveLevelId))
        {
            var elevation = (double)level.ElevationMillimetres;
            var color = level.Id == ActiveLevelId ? Color.Parse("#79C7E8") : Color.Parse("#587F72");
            var pen = new Pen(new SolidColorBrush(color), level.Id == ActiveLevelId ? 3.2 : 2);
            foreach (var segment in Engine.Segments.Where(s => s.LevelId == level.Id))
            {
                var points = BuildSegmentPoints(segment);
                for (var i = 1; i < points.Count; i++)
                    context.DrawLine(pen, Project(points[i - 1].X, -points[i - 1].Y, elevation + 8, center),
                        Project(points[i].X, -points[i].Y, elevation + 8, center));
                if (segment.Kind == TrackGeometryKind.Turnout)
                {
                    var dx = segment.EndX - segment.StartX;
                    var dy = segment.EndY - segment.StartY;
                    var a = (segment.StartX + dx * 0.48m, segment.StartY + dy * 0.48m);
                    var b = (segment.EndX - dx * 0.42m - dy * 0.28m, segment.EndY - dy * 0.42m + dx * 0.28m);
                    context.DrawLine(pen, Project((double)a.Item1, -(double)a.Item2, elevation + 8, center),
                        Project((double)b.Item1, -(double)b.Item2, elevation + 8, center));
                }
            }
            foreach (var piece in Engine.Pieces.Where(p => p.LevelId == level.Id))
                DrawCataloguePiece(context, piece, elevation, center, color);
        }
    }

    private void DrawCataloguePiece(DrawingContext context, TrackPiece piece, double elevation, Point center, Color levelColor)
    {
        var item = CatalogueItems.FirstOrDefault(x => x.Ref.Equals(piece.CatalogueCode, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            var anchor = Project((double)piece.X, -(double)piece.Y, elevation + 8, center);
            context.DrawEllipse(new SolidColorBrush(Color.Parse("#E59A45")), null, anchor, 5, 5);
            return;
        }
        var ports = item.Ports.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var pen = new Pen(new SolidColorBrush(item.GeometryStatus == "confirmed" ? levelColor : Color.Parse("#F4A261")), 2.6);
        foreach (var route in item.TrackPaths)
        {
            if (!ports.TryGetValue(route.FromPort, out var from) || !ports.TryGetValue(route.ToPort, out var to)) continue;
            var points = RoutePoints(piece, route, from, to);
            for (var i = 1; i < points.Count; i++)
                context.DrawLine(pen, Project((double)points[i - 1].X, -(double)points[i - 1].Y, elevation + 8, center),
                    Project((double)points[i].X, -(double)points[i].Y, elevation + 8, center));
        }
        if (item.TrackPaths.Count == 0)
        {
            var anchor = Project((double)piece.X, -(double)piece.Y, elevation + 8, center);
            context.DrawEllipse(new SolidColorBrush(Color.Parse("#E59A45")), null, anchor, 5, 5);
        }
        foreach (var port in item.Ports)
        {
            var world = TrackEditorEngine.TransformPort(piece, port.X, port.Y);
            var p = Project((double)world.X, -(double)world.Y, elevation + 8, center);
            context.DrawEllipse(new SolidColorBrush(port.Accuracy == "estimated" ? Color.Parse("#F4A261") : Color.Parse("#7AE582")),
                null, p, 2.8, 2.8);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _orbiting = true;
            _lastPointer = e.GetPosition(this);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_orbiting || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var point = e.GetPosition(this);
        var delta = point - _lastPointer;
        _lastPointer = point;
        _yaw += delta.X * 0.45;
        _pitch = Math.Clamp(_pitch - delta.Y * 0.35, 5, 88);
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _orbiting = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _zoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.12 : 0.89), 0.08, 1.4);
        InvalidateVisual();
        e.Handled = true;
    }

    private Point Project(double x, double y, double z, Point center)
    {
        var yaw = _yaw * Math.PI / 180d;
        var pitch = _pitch * Math.PI / 180d;
        var rx = x * Math.Cos(yaw) - y * Math.Sin(yaw);
        var ry = x * Math.Sin(yaw) + y * Math.Cos(yaw);
        return new Point(center.X + rx * _zoom,
            center.Y + (ry * Math.Sin(pitch) - z * Math.Cos(pitch)) * _zoom);
    }

    private static IReadOnlyList<(decimal X, decimal Y)> RoutePoints(TrackPiece piece,
        CatalogueTrackPath route, CataloguePort from, CataloguePort to)
    {
        var start = TrackEditorEngine.TransformPort(piece, from.X, from.Y);
        var end = TrackEditorEngine.TransformPort(piece, to.X, to.Y);
        if (!route.Kind.Equals("quadratic", StringComparison.OrdinalIgnoreCase))
            return [start, end];
        var control = TrackEditorEngine.TransformPort(piece, route.ControlX, route.ControlY);
        var points = new List<(decimal X, decimal Y)>();
        for (var i = 0; i <= 24; i++)
        {
            var t = i / 24m;
            var u = 1m - t;
            points.Add((u * u * start.X + 2m * u * t * control.X + t * t * end.X,
                u * u * start.Y + 2m * u * t * control.Y + t * t * end.Y));
        }
        return points;
    }

    private static IReadOnlyList<(double X, double Y)> BuildSegmentPoints(TrackGeometrySegment s)
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