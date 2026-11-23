using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Platinum.Domain.Layout;

namespace Platinum.Presentation.Avalonia.Views;

/// <summary>Interactive 3D projection preview using the same domain centre-lines and level elevations.</summary>
public sealed class Track3DCanvas : Control
{
    private double _yaw = -42;
    private double _pitch = 42;
    private double _zoom = 0.34;
    private Point _lastPointer;
    private bool _orbiting;

    public TrackEditorEngine? Engine { get; set; }
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
            var a = Project(i * 100, -1000, -8, center);
            var b = Project(i * 100, 1000, -8, center);
            context.DrawLine(gridPen, a, b);
            var c = Project(-1000, i * 100, -8, center);
            var d = Project(1000, i * 100, -8, center);
            context.DrawLine(gridPen, c, d);
        }

        if (Engine is null) return;
        foreach (var level in Levels.Where(l => ShowAllLevels || l.Id == ActiveLevelId))
        {
            var elevation = (double)level.ElevationMillimetres;
            var color = level.Id == ActiveLevelId ? Color.Parse("#79C7E8") : Color.Parse("#587F72");
            var pen = new Pen(new SolidColorBrush(color), level.Id == ActiveLevelId ? 3.2 : 2);
            foreach (var segment in Engine.Segments.Where(s => s.LevelId == level.Id))
            {
                var points = BuildSegmentPoints(segment);
                for (var i = 1; i < points.Count; i++)
                    context.DrawLine(pen, Project(points[i - 1].X, -points[i - 1].Y, elevation, center),
                        Project(points[i].X, -points[i].Y, elevation, center));
            }
            foreach (var piece in Engine.Pieces.Where(p => p.LevelId == level.Id))
            {
                var p = Project((double)piece.X, -(double)piece.Y, elevation + 10, center);
                context.DrawEllipse(new SolidColorBrush(Color.Parse("#E59A45")), null, p, 5.5, 5.5);
            }
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