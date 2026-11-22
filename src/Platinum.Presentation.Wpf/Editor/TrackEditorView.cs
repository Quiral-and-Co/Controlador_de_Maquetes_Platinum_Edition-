using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Platinum.Domain.Layout;
using Platinum.Presentation.Wpf.Catalogue;

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
    private readonly List<TrackPiece> _pieces = [];
    private readonly List<TrackConnection> _connections = [];
    private readonly Dictionary<string, CatalogueItem> _catalogueByCode = new(StringComparer.OrdinalIgnoreCase);
    private CatalogueItem? _placementItem;
    private Guid? _selectedPieceId;
    private Guid? _pendingConnectionPieceId;
    private string? _pendingConnectionPortId;
    private bool _connectMode;
    private readonly Stack<EditorSnapshot> _undo = new();
    private readonly Stack<EditorSnapshot> _redo = new();

    private sealed record EditorSnapshot(
        TrackGeometrySegment[] Segments,
        TrackPiece[] Pieces,
        TrackConnection[] Connections);
    private Guid _levelId;
    private Tool _tool = Tool.Select;
    private Point? _anchor;
    private Point? _lastPointer;
    private Guid? _selectedId;
    private bool _draggingSelection;
    private double _zoomFactor = 1d;
    private bool _snapToGrid = true;
    private decimal _gridMillimetres = 10m;

    private enum Tool { Select, Straight, Curve, Turnout }

    public TrackEditorView() : this(Guid.Empty) { }

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
        AddButton(toolbar, "Girar −15°", (_, _) => RotateSelected(-15m));
        AddButton(toolbar, "Girar +15°", (_, _) => RotateSelected(15m));
        AddButton(toolbar, "Girar 90°", (_, _) => RotateSelected(90m));
        AddButton(toolbar, "Connectar peces", (_, _) => ToggleConnectMode());
        var snap = new CheckBox { Content = "Ajustar a graella (10 mm)", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Foreground = Brushes.LightGray };
        snap.Checked += (_, _) => _snapToGrid = true;
        snap.Unchecked += (_, _) => _snapToGrid = false;
        toolbar.Children.Add(snap);
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
    public IReadOnlyList<TrackPiece> Pieces => _pieces;
    public IReadOnlyList<TrackConnection> Connections => _connections;
    public event Action<TrackPiece>? CataloguePiecePlaced;
    public event Action<TrackConnection>? ConnectionCreated;
    public event Action? ProjectModelChanged;

    public void SetActiveLevel(Guid levelId)
    {
        _levelId = levelId;
        _selectedId = null;
        _selectedPieceId = null;
        _pendingConnectionPieceId = null;
        _pendingConnectionPortId = null;
        _connectMode = false;
        _anchor = null;
        _draggingSelection = false;
        Draw();
    }

    public void SetCatalogue(IEnumerable<CatalogueItem> items)
    {
        _catalogueByCode.Clear();
        foreach (var item in items) _catalogueByCode[item.Ref] = item;
        Draw();
    }

    public void LoadPieces(IEnumerable<TrackPiece>? pieces)
    {
        _pieces.Clear();
        if (pieces is not null) _pieces.AddRange(pieces);
        _selectedPieceId = null;
        Draw();
    }

    public void LoadConnections(IEnumerable<TrackConnection>? connections)
    {
        _connections.Clear();
        if (connections is not null) _connections.AddRange(connections);
        _pendingConnectionPieceId = null;
        _pendingConnectionPortId = null;
        Draw();
    }

    private void RotateSelected(decimal degrees)
    {
        if (!_selectedPieceId.HasValue) { _hint.Text = "Selecciona una peça del catàleg abans de girar-la."; return; }
        var index = _pieces.FindIndex(piece => piece.Id == _selectedPieceId.Value);
        if (index < 0) return;
        PushUndo();
        var piece = _pieces[index];
        _pieces[index] = piece with { RotationDegrees = ((piece.RotationDegrees + degrees) % 360m + 360m) % 360m };
        ProjectModelChanged?.Invoke();
        _hint.Text = $"Peça {piece.CatalogueCode}: girada a {_pieces[index].RotationDegrees:0.#}°.";
        Draw();
    }

    private void ToggleConnectMode()
    {
        _connectMode = !_connectMode;
        _pendingConnectionPieceId = null;
        _pendingConnectionPortId = null;
        _placementItem = null;
        _hint.Text = _connectMode
            ? "Connexió per ports: clica els punts verds dels extrems. Els taronges són estimats; cap connexió es marca com a verificada físicament."
            : "Mode de connexió desactivat.";
    }

    public void BeginCataloguePlacement(CatalogueItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _placementItem = item;
        _tool = Tool.Select;
        _anchor = null;
        _draggingSelection = false;
        _hint.Text = $"Col·loca {item.Ref} fent clic al nivell actiu. Símbol esquemàtic, no a escala; estat geomètric: {item.GeometryStatus}.";
    }

    public void SetGridSize(decimal millimetres)
    {
        if (millimetres <= 0 || millimetres > 1000)
            throw new ArgumentOutOfRangeException(nameof(millimetres), "La graella ha d'estar entre 0 i 1000 mm.");
        _gridMillimetres = millimetres;
    }

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

    private Point ToModel(Point viewPoint) => new((viewPoint.X / BaseScale) - 80, (viewPoint.Y / BaseScale) - 80);

    private Point Snap(Point point)
    {
        if (!_snapToGrid) return point;
        return new Point(Math.Round(point.X / (double)_gridMillimetres) * (double)_gridMillimetres,
            Math.Round(point.Y / (double)_gridMillimetres) * (double)_gridMillimetres);
    }
    private Point ToView(decimal x, decimal y) => new(((double)x + 80) * BaseScale, ((double)y + 80) * BaseScale);

    private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var point = Snap(ToModel(e.GetPosition(_surface)));
        if (_placementItem is not null)
        {
            var item = _placementItem;
            PushUndo();
            var placed = new TrackPiece(Guid.NewGuid(), item.Ref, (decimal)point.X, (decimal)point.Y, 0m, _levelId, item.GeometryStatus);
            _pieces.Add(placed);
            _placementItem = null;
            CataloguePiecePlaced?.Invoke(placed);
            ProjectModelChanged?.Invoke();
            _hint.Text = $"Peça {item.Ref} col·locada al nivell actiu. La geometria es conserva amb estat {item.GeometryStatus}.";
            Draw();
            e.Handled = true;
            return;
        }
        if (_connectMode)
        {
            var hit = HitTestPort(point);
            if (hit is null)
            {
                _hint.Text = "Clica a prop d'un extrem de via marcat. Els ports taronges són estimats.";
                return;
            }

            var pieceId = hit.Value.Piece.Id;
            var portId = hit.Value.Port.Id;
            if (!_pendingConnectionPieceId.HasValue)
            {
                _pendingConnectionPieceId = pieceId;
                _pendingConnectionPortId = portId;
                _selectedPieceId = pieceId;
                _hint.Text = $"Port {portId} de {hit.Value.Piece.CatalogueCode} seleccionat. Ara tria un port d'una altra peça.";
            }
            else if (_pendingConnectionPieceId.Value == pieceId && _pendingConnectionPortId == portId)
            {
                _hint.Text = "Aquest és el mateix port. Tria un altre extrem.";
            }
            else
            {
                var fromPiece = _pendingConnectionPieceId.Value;
                var fromPort = _pendingConnectionPortId ?? "logical";
                var exists = _connections.Any(connection =>
                    (connection.FromPieceId == fromPiece && connection.FromPort == fromPort && connection.ToPieceId == pieceId && connection.ToPort == portId) ||
                    (connection.FromPieceId == pieceId && connection.FromPort == portId && connection.ToPieceId == fromPiece && connection.ToPort == fromPort));
                if (!exists)
                {
                    var accuracy = hit.Value.Port.Accuracy;
                    var sourcePort = FindPort(fromPiece, fromPort);
                    if (sourcePort?.Accuracy == "estimated" || accuracy == "estimated")
                        _hint.Text = "Connexió entre ports creada; un dels extrems és estimat i cal verificar-lo amb plànol Märklin.";
                    else
                        _hint.Text = "Connexió entre extrems creada a partir de dimensions de catàleg; no certifica l'encaix físic.";
                    PushUndo();
                    var connection = new TrackConnection(Guid.NewGuid(), fromPiece, fromPort, pieceId, portId, false);
                    _connections.Add(connection);
                    ConnectionCreated?.Invoke(connection);
                    ProjectModelChanged?.Invoke();
                }
                else _hint.Text = "Aquests dos ports ja estan connectats.";
                _pendingConnectionPieceId = null;
                _pendingConnectionPortId = null;
            }
            Draw();
            e.Handled = true;
            return;
        }

        if (_tool == Tool.Select)
        {
            _selectedPieceId = HitTestPiece(point);
            if (_selectedPieceId.HasValue)
            {
                PushUndo();
                _selectedId = null;
                _lastPointer = point;
                _draggingSelection = true;
                _surface.CaptureMouse();
                _hint.Text = "Peça seleccionada: arrossega per moure-la; usa els botons de gir.";
                Draw();
                e.Handled = true;
                return;
            }
            _selectedPieceId = null;
            _selectedId = HitTest(point);
            _lastPointer = point;
            if (_selectedId.HasValue) PushUndo();
            _draggingSelection = _selectedId.HasValue;
            if (_draggingSelection) _surface.CaptureMouse();
            Draw();
            return;
        }

        _anchor = Snap(point);
        _surface.CaptureMouse();
        e.Handled = true;
    }

    private void Surface_MouseMove(object sender, MouseEventArgs e)
    {
        var point = ToModel(e.GetPosition(_surface));
        if (_draggingSelection && (_selectedPieceId.HasValue || _selectedId.HasValue) && _lastPointer.HasValue && e.LeftButton == MouseButtonState.Pressed)
        {
            var snappedPoint = Snap(point);
            var previousPoint = Snap(_lastPointer.Value);
            var dx = (decimal)(snappedPoint.X - previousPoint.X);
            var dy = (decimal)(snappedPoint.Y - previousPoint.Y);
            var pieceIndex = _selectedPieceId.HasValue ? _pieces.FindIndex(piece => piece.Id == _selectedPieceId.Value) : -1;
            if (pieceIndex >= 0)
            {
                var piece = _pieces[pieceIndex];
                _pieces[pieceIndex] = piece with { X = piece.X + dx, Y = piece.Y + dy };
                _lastPointer = point;
                ProjectModelChanged?.Invoke();
                Draw();
            }
            var index = _selectedId.HasValue ? _segments.FindIndex(s => s.Id == _selectedId.Value) : -1;
            if (index >= 0)
            {
                var s = _segments[index];
                _segments[index] = s with { StartX = s.StartX + dx, StartY = s.StartY + dy, EndX = s.EndX + dx, EndY = s.EndY + dy, ControlX = s.ControlX + dx, ControlY = s.ControlY + dy };
                _lastPointer = point;
                ProjectModelChanged?.Invoke();
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
        point = Snap(point);
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
        ProjectModelChanged?.Invoke();
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

    private EditorSnapshot CaptureSnapshot() => new(_segments.ToArray(), _pieces.ToArray(), _connections.ToArray());

    private void RestoreSnapshot(EditorSnapshot snapshot)
    {
        _segments.Clear();
        _segments.AddRange(snapshot.Segments);
        _pieces.Clear();
        _pieces.AddRange(snapshot.Pieces);
        _connections.Clear();
        _connections.AddRange(snapshot.Connections);
        _selectedId = null;
        _selectedPieceId = null;
        _pendingConnectionPieceId = null;
        _pendingConnectionPortId = null;
    }

    private void PushUndo()
    {
        _undo.Push(CaptureSnapshot());
        _redo.Clear();
    }

    private void Undo()
    {
        if (_undo.Count == 0) return;
        _redo.Push(CaptureSnapshot());
        RestoreSnapshot(_undo.Pop());
        ProjectModelChanged?.Invoke();
        Draw();
    }

    private void Redo()
    {
        if (_redo.Count == 0) return;
        _undo.Push(CaptureSnapshot());
        RestoreSnapshot(_redo.Pop());
        ProjectModelChanged?.Invoke();
        Draw();
    }

    private CataloguePort? FindPort(Guid pieceId, string portId)
    {
        var piece = _pieces.FirstOrDefault(candidate => candidate.Id == pieceId);
        if (piece is null || !_catalogueByCode.TryGetValue(piece.CatalogueCode, out var item)) return null;
        return item.Ports.FirstOrDefault(port => string.Equals(port.Id, portId, StringComparison.OrdinalIgnoreCase));
    }

    private Point GetWorldPort(TrackPiece piece, string portId)
    {
        if (portId == "logical" || !_catalogueByCode.TryGetValue(piece.CatalogueCode, out var item))
            return new Point((double)piece.X, (double)piece.Y);
        var port = item.Ports.FirstOrDefault(candidate => string.Equals(candidate.Id, portId, StringComparison.OrdinalIgnoreCase));
        if (port is null) return new Point((double)piece.X, (double)piece.Y);

        var radians = (double)piece.RotationDegrees * Math.PI / 180d;
        var x = (double)port.X;
        var y = (double)port.Y;
        return new Point(
            (double)piece.X + x * Math.Cos(radians) - y * Math.Sin(radians),
            (double)piece.Y + x * Math.Sin(radians) + y * Math.Cos(radians));
    }

    private (TrackPiece Piece, CataloguePort Port)? HitTestPort(Point point)
    {
        const double toleranceMm = 22d;
        (TrackPiece Piece, CataloguePort Port)? nearest = null;
        var nearestDistance = toleranceMm;
        foreach (var piece in _pieces.Where(candidate => candidate.LevelId == _levelId).Reverse())
        {
            if (!_catalogueByCode.TryGetValue(piece.CatalogueCode, out var item)) continue;
            foreach (var port in item.Ports)
            {
                var world = GetWorldPort(piece, port.Id);
                var distance = (point - world).Length;
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = (piece, port);
                }
            }
        }
        return nearest;
    }

    private void DrawPiecePorts(TrackPiece piece)
    {
        if (!_catalogueByCode.TryGetValue(piece.CatalogueCode, out var item)) return;
        foreach (var port in item.Ports)
        {
            var world = GetWorldPort(piece, port.Id);
            var view = ToView((decimal)world.X, (decimal)world.Y);
            var estimated = port.Accuracy == "estimated";
            var marker = new System.Windows.Shapes.Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = estimated ? Brushes.Orange : Brushes.LimeGreen,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                IsHitTestVisible = false,
                ToolTip = $"Port {port.Id} · {port.Accuracy} · coordenades locals en mm"
            };
            Canvas.SetLeft(marker, view.X - 4.5);
            Canvas.SetTop(marker, view.Y - 4.5);
            _surface.Children.Add(marker);
        }
    }

    private Guid? HitTestPiece(Point point)
    {
        const double tolerance = 115;
        foreach (var piece in _pieces.Where(piece => piece.LevelId == _levelId).Reverse())
        {
            var center = new Point((double)piece.X, (double)piece.Y);
            if ((point - center).Length <= tolerance) return piece.Id;
        }
        return null;
    }

    private Guid? HitTest(Point point)
    {
        const double tolerance = 55;
        foreach (var segment in _segments.Where(s => s.LevelId == _levelId).Reverse())
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
        foreach (var connection in _connections)
        {
            var from = _pieces.FirstOrDefault(piece => piece.Id == connection.FromPieceId && piece.LevelId == _levelId);
            var to = _pieces.FirstOrDefault(piece => piece.Id == connection.ToPieceId && piece.LevelId == _levelId);
            if (from is null || to is null) continue;
            var fromPoint = GetWorldPort(from, connection.FromPort);
            var toPoint = GetWorldPort(to, connection.ToPort);
            var a = ToView((decimal)fromPoint.X, (decimal)fromPoint.Y);
            var b = ToView((decimal)toPoint.X, (decimal)toPoint.Y);
            _surface.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y,
                Stroke = connection.PhysicallyVerified ? Brushes.LimeGreen : Brushes.Orange,
                StrokeThickness = 2,
                StrokeDashArray = connection.PhysicallyVerified ? null : new DoubleCollection { 2, 3 },
                IsHitTestVisible = false,
                ToolTip = connection.PhysicallyVerified ? "Connexió verificada físicament" : "Connexió lògica pendent de verificació física"
            });
        }
        foreach (var piece in _pieces.Where(p => p.LevelId == _levelId))
        {
            DrawCataloguePiece(piece);
            DrawPiecePorts(piece);
        }
        foreach (var segment in _segments.Where(s => s.LevelId == _levelId))
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

    private void DrawCataloguePiece(TrackPiece piece)
    {
        var origin = ToView(piece.X, piece.Y);
        var itemExists = _catalogueByCode.TryGetValue(piece.CatalogueCode, out var item);
        var status = itemExists ? item!.GeometryStatus : piece.GeometryStatus;
        var color = status switch
        {
            "confirmed" => Color.FromRgb(83, 190, 210),
            "partial" => Color.FromRgb(240, 174, 82),
            _ => Color.FromRgb(165, 174, 184)
        };

        var trackGeometry = new GeometryGroup();
        if (item is not null)
        {
            var ports = item.Ports.ToDictionary(port => port.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var route in item.TrackPaths)
            {
                if (!ports.TryGetValue(route.FromPort, out var from) ||
                    !ports.TryGetValue(route.ToPort, out var to))
                    continue;

                var start = new Point((double)from.X * BaseScale, (double)from.Y * BaseScale);
                var end = new Point((double)to.X * BaseScale, (double)to.Y * BaseScale);
                var figure = new PathFigure { StartPoint = start, IsClosed = false };
                if (route.Kind.Equals("quadratic", StringComparison.OrdinalIgnoreCase))
                {
                    var control = new Point((double)route.ControlX * BaseScale, (double)route.ControlY * BaseScale);
                    figure.Segments.Add(new QuadraticBezierSegment(control, end, true));
                }
                else
                {
                    figure.Segments.Add(new LineSegment(end, true));
                }
                trackGeometry.Children.Add(new PathGeometry { Figures = new PathFigureCollection { figure } });
            }
        }

        // For a terminal/unsupported special item with no centre-line route,
        // retain a visible anchor glyph rather than inventing track geometry.
        if (trackGeometry.Children.Count == 0)
            trackGeometry.Children.Add(new EllipseGeometry(new Point(0, 0), 5, 5));

        var radians = (double)piece.RotationDegrees * Math.PI / 180d;
        var transform = new MatrixTransform(new Matrix(
            Math.Cos(radians), Math.Sin(radians),
            -Math.Sin(radians), Math.Cos(radians),
            origin.X, origin.Y));

        var path = new System.Windows.Shapes.Path
        {
            Data = trackGeometry,
            RenderTransform = transform,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = piece.Id == _selectedPieceId ? 7 : 5,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeDashArray = status == "confirmed" ? null : new DoubleCollection { 3, 2 },
            IsHitTestVisible = false,
            ToolTip = $"{piece.CatalogueCode} · {item?.Name ?? "Peça de catàleg"} · geometria {status} · traçats connectats als ports"
        };
        _surface.Children.Add(path);

        var label = new TextBlock
        {
            Text = piece.CatalogueCode,
            Foreground = new SolidColorBrush(color),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(label, origin.X + 25);
        Canvas.SetTop(label, origin.Y - 9);
        _surface.Children.Add(label);
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
                    group.Children.Add(new PathGeometry { Figures = new PathFigureCollection { figure } });
                    group.Children.Add(new PathGeometry { Figures = new PathFigureCollection { branch } });
                    return group;
                }
            }
        }
        return new PathGeometry { Figures = new PathFigureCollection { figure } };
    }
}
