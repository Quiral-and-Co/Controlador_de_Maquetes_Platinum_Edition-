using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Platinum.Domain.Layout;

namespace Platinum.Presentation.Wpf.Editor;

/// <summary>
/// First native WPF track-plan editor: straight, quadratic curve and turnout drawing,
/// selection/move, wheel zoom, and local undo/redo. All coordinates are stored in mm.
/// </summary>
public sealed class TrackEditorView : UserControl
{
    private const double BaseScale = 0.22;
    private readonly Canvas _surface = new() { Width = 1900, Height = 1150, Background = new SolidColorBrush(Color.FromRgb(18, 27, 36)), ClipToBounds = true };
    private readonly ScaleTransform _zoom = new(1, 1);
    private readonly TextBlock _hint = new() { Foreground = Brushes.LightGray, Margin = new Thickness(10, 4, 0, 4) };
    private readonly List<TrackGeometrySegment> _segments = [];
    private readonly Stack<List<TrackGeometrySegment>> _undo = new();
    private readonly Stack<List<TrackGeometrySegment>> _redo = new();
    private readonly Guid _levelId;
    private Tool _tool = Tool.Select;
    private Point? _anchor;
    private Point? _lastPointer;
    private Guid? _selectedId;
    private bool _draggingSelection;
    private double _zoomFactor = 1d;

    private enum Tool { Select, Straight, Curve, Turnout }

    public TrackEditorView(Guid levelId)
    {
        _levelId = levelId;
        var root = new DockPanel();
        var toolbar = new WrapPanel { Margin = new Thickness(8) };
        AddTool(toolbar, "Seleccionar", Tool.Select);
        AddTool(toolbar, "Via recta", Tool.Straight);
        AddTool(toolbar, "Corba", Tool.Curve);
        AddTool(toolbar, "Desviament", Tool.Turnout);
        AddButton(toolbar, "Desfer", (_, _) => Undo());
        AddButton(toolbar, "Refer", (_, _) => Redo());
        AddButton(toolbar, "Zoom +", (_, _) => SetZoom(_zoomFactor * 1.2));
        AddButton(toolbar, "Zoom −", (_, _) => SetZoom(_zoomFactor / 1.2));
        AddButton(toolbar, "Ajustar", (_, _) => SetZoom(1));
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);

        var scroller = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _surface.RenderTransform = _zoom;
        _surface.RenderTransformOrigin = new Point(0, 0);
        _surface.MouseLeftButtonDown += Surface_MouseLeftButtonDown;
        _surface.MouseMove += Surface_MouseMove;
        _surface.MouseLeftButtonUp += Surface_MouseLeftButtonUp;
        _surface.MouseWheel += Surface_MouseWheel;
        scroller.Content = _surface;
        root.Children.Add(scroller);
        DockPanel.SetDock(_hint, Dock.Bottom);
        root.Children.Add(_hint);
        Content = root;
        Draw();
        SetTool(Tool.Select);
    }

    public IReadOnlyList<TrackGeometrySegment> Segments => _segments;

    public void LoadSegments(IEnumerable<TrackGeometrySegment>? segments)
    {
        _segments.Clear();
        if (segments is not null) _segments.AddRange(segments);
        _undo.Clear();
        _redo.Clear();
        _selectedId = null;
        Draw();
    }

    private void AddTool(Panel panel, string label, Tool tool) => AddButton(panel, label, (_, _) => SetTool(tool));
    private static void AddButton(Panel panel, string label, RoutedEventHandler handler)
    {
        var button = new Button { Content = label, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 6, 0), MinWidth = 78 };
        button.Click += handler;
        panel.Children.Add(button);
    }

    private void SetTool(Tool tool)
    {
        _tool = tool;
        _anchor = null;
        _draggingSelection = false;
        _hint.Text = tool switch
        {
            Tool.Select => "Selecció: clica una via i arrossega-la. Roda del ratolí: zoom.",
            Tool.Straight => "Via recta: arrossega des del punt inicial fins al punt final.",
            Tool.Curve => "Corba: arrossega per definir els extrems; el control crea una corba suau.",
            Tool.Turnout => "Desviament: arrossega per definir la via principal i la branca.",
            _ => string.Empty
        };
    }

    private Point ToModel(Point viewPoint) => new((viewPoint.X / (BaseScale * _zoomFactor)) - 80, (viewPoint.Y / (BaseScale * _zoomFactor)) - 80);
    private Point ToView(decimal x, decimal y) => new(((double)x + 80) * BaseScale, ((double)y + 80) * BaseScale);

    private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var point = ToModel(e.GetPosition(_surface));
        if (_tool == Tool.Select)
        {
            _selectedId = HitTest(point);
            _lastPointer = point;
            _draggingSelection = _selectedId.HasValue;
            if (_draggingSelection) _surface.CaptureMouse();
            Draw();
            return;
        }

        _anchor = point;
        _surface.CaptureMouse();
        e.Handled = true;
    }

    private void Surface_MouseMove(object sender, MouseEventArgs e)
    {
        var point = ToModel(e.GetPosition(_surface));
        if (_draggingSelection && _selectedId.HasValue && _lastPointer.HasValue && e.LeftButton == MouseButtonState.Pressed)
        {
            var dx = (decimal)(point.X - _lastPointer.Value.X);
            var dy = (decimal)(point.Y - _lastPointer.Value.Y);
            var index = _segments.FindIndex(s => s.Id == _selectedId.Value);
            if (index >= 0)
            {
                var s = _segments[index];
                _segments[index] = s with { StartX = s.StartX + dx, StartY = s.StartY + dy, EndX = s.EndX + dx, EndY = s.EndY + dy, ControlX = s.ControlX + dx, ControlY = s.ControlY + dy };
                _lastPointer = point;
                Draw();
            }
        }
        else if (_anchor.HasValue && e.LeftButton == MouseButtonState.Pressed)
        {
            DrawPreview(_anchor.Value, point);
        }
    }

    private void Surface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var point = ToModel(e.GetPosition(_surface));
        if (_draggingSelection)
        {
            _draggingSelection = false;
            _surface.ReleaseMouseCapture();
            _lastPointer = null;
            return;
        }
        if (!_anchor.HasValue) return;
        var start = _anchor.Value;
        _anchor = null;
        _surface.ReleaseMouseCapture();
        if ((point - start).Length < 8) { Draw(); return; }

        PushUndo();
        var dx = point.X - start.X;
        var dy = point.Y - start.Y;
        var segment = new TrackGeometrySegment(
            Guid.NewGuid(),
            _tool switch { Tool.Curve => TrackGeometryKind.Curve, Tool.Turnout => TrackGeometryKind.Turnout, _ => TrackGeometryKind.Straight },
            (decimal)start.X, (decimal)start.Y, (decimal)point.X, (decimal)point.Y,
            (decimal)(start.X + dx / 2 - dy * 0.22), (decimal)(start.Y + dy / 2 + dx * 0.22),
            _levelId);
        _segments.Add(segment);
        _selectedId = segment.Id;
        Draw();
    }

    private void Surface_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        SetZoom(_zoomFactor * (e.Delta > 0 ? 1.15 : 1 / 1.15));
        e.Handled = true;
    }

    private void SetZoom(double value)
    {
        _zoomFactor = Math.Clamp(value, 0.35, 3.5);
        _zoom.ScaleX = _zoomFactor;
        _zoom.ScaleY = _zoomFactor;
        Draw();
    }

    private void PushUndo()
    {
        _undo.Push(_segments.ToList());
        _redo.Clear();
    }

    private void Undo()
    {
        if (_undo.Count == 0) return;
        _redo.Push(_segments.ToList());
        _segments.Clear();
        _segments.AddRange(_undo.Pop());
        _selectedId = null;
        Draw();
    }

    private void Redo()
    {
        if (_redo.Count == 0) return;
        _undo.Push(_segments.ToList());
        _segments.Clear();
        _segments.AddRange(_redo.Pop());
        _selectedId = null;
        Draw();
    }

    private Guid? HitTest(Point point)
    {
        const double tolerance = 55;
        foreach (var segment in _segments.AsEnumerable().Reverse())
        {
            var a = new Point((double)segment.StartX, (double)segment.StartY);
            var b = new Point((double)segment.EndX, (double)segment.EndY);
            if (DistanceToSegment(point, a, b) <= tolerance) return segment.Id;
            if (segment.Kind == TrackGeometryKind.Curve)
            {
                var c = new Point((double)segment.ControlX, (double)segment.ControlY);
                if (DistanceToSegment(point, a, c) <= tolerance || DistanceToSegment(point, c, b) <= tolerance) return segment.Id;
            }
        }
        return null;
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        var ab = b - a;
        var denominator = ab.X * ab.X + ab.Y * ab.Y;
        if (denominator <= double.Epsilon) return (p - a).Length;
        var t = Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / denominator, 0, 1);
        return (p - (a + t * ab)).Length;
    }

    private void DrawPreview(Point start, Point end)
    {
        Draw();
        var a = ToView((decimal)start.X, (decimal)start.Y);
        var b = ToView((decimal)end.X, (decimal)end.Y);
        var preview = new System.Windows.Shapes.Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = Brushes.Gold, StrokeThickness = 5, StrokeDashArray = [3, 2], IsHitTestVisible = false };
        _surface.Children.Add(preview);
    }

    private void Draw()
    {
        _surface.Children.Clear();
        var grid = new DrawingGroup();
        using (var dc = grid.Open())
        {
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(39, 53, 67)), 0.7);
            for (var x = 0; x < _surface.Width; x += 22) dc.DrawLine(pen, new Point(x, 0), new Point(x, _surface.Height));
            for (var y = 0; y < _surface.Height; y += 22) dc.DrawLine(pen, new Point(0, y), new Point(_surface.Width, y));
        }
        _surface.Background = new DrawingBrush(grid) { Stretch = Stretch.None };
        foreach (var segment in _segments)
        {
            var geometry = CreateGeometry(segment);
            var selected = segment.Id == _selectedId;
            var path = new System.Windows.Shapes.Path
            {
                Data = geometry,
                Stroke = selected ? Brushes.Gold : new SolidColorBrush(Color.FromRgb(222, 230, 236)),
                StrokeThickness = selected ? 7 : 5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };
            _surface.Children.Add(path);
        }
    }

    private static Geometry CreateGeometry(TrackGeometrySegment segment)
    {
        var start = new Point(((double)segment.StartX + 80) * BaseScale, ((double)segment.StartY + 80) * BaseScale);
        var end = new Point(((double)segment.EndX + 80) * BaseScale, ((double)segment.EndY + 80) * BaseScale);
        var figure = new PathFigure { StartPoint = start, IsClosed = false };
        if (segment.Kind == TrackGeometryKind.Curve)
        {
            figure.Segments.Add(new QuadraticBezierSegment(
                new Point(((double)segment.ControlX + 80) * BaseScale, ((double)segment.ControlY + 80) * BaseScale), end, true));
        }
        else
        {
            figure.Segments.Add(new LineSegment(end, true));
            if (segment.Kind == TrackGeometryKind.Turnout)
            {
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                var length = Math.Sqrt(dx * dx + dy * dy);
                if (length > 0)
                {
                    var branchEnd = new Point(end.X - dx * 0.42 - dy * 0.28, end.Y - dy * 0.42 + dx * 0.28);
                    var branch = new PathFigure { StartPoint = new Point(start.X + dx * 0.48, start.Y + dy * 0.48), IsClosed = false };
                    branch.Segments.Add(new LineSegment(branchEnd, true));
                    var group = new GeometryGroup();
                    group.Children.Add(new PathGeometry([figure]));
                    group.Children.Add(new PathGeometry([branch]));
                    return group;
                }
            }
        }
        return new PathGeometry([figure]);
    }
}
