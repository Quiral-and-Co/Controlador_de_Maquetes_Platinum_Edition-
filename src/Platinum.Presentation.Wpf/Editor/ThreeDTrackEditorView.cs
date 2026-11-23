using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Platinum.Domain.Layout;
using Platinum.Domain.Catalogue;

namespace Platinum.Presentation.Wpf.Editor;

/// <summary>
/// Native WPF 3D layout editor/preview. Uses the same centre-line data and
/// catalogue port paths as the 2D editor; no browser, WebView or JMRI dependency.
/// </summary>
public sealed class ThreeDTrackEditorView : UserControl
{
    private readonly Viewport3D _viewport = new();
    private readonly PerspectiveCamera _camera = new(new Point3D(0, -1800, 1700), new Vector3D(0, 0.65, -0.55), new Vector3D(0, 0, 1), 55);
    private readonly Model3DGroup _scene = new();
    private readonly TextBlock _status = new() { Foreground = Brushes.Gainsboro, Margin = new Thickness(10, 5, 10, 5) };
    private readonly Dictionary<string, CatalogueItem> _catalogue = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<TrackGeometrySegment> _segments = [];
    private IReadOnlyList<TrackPiece> _pieces = [];
    private IReadOnlyList<TrackLevel> _levels = [];
    private Guid _activeLevel;
    private Point _lastMouse;
    private bool _orbiting;
    private double _yaw = -42;
    private double _pitch = 42;
    private double _distance = 2200;
    private bool _showAllLevels = true;
    private static readonly Color[] LevelColors = [Colors.LightSkyBlue, Colors.LightGreen, Colors.Khaki, Colors.Plum, Colors.Coral, Colors.Turquoise];

    public ThreeDTrackEditorView()
    {
        var root = new DockPanel { Background = new SolidColorBrush(Color.FromRgb(16, 23, 31)) };
        var toolbar = new WrapPanel { Margin = new Thickness(8) };
        AddButton(toolbar, "Vista superior", (_, _) => { _yaw = 0; _pitch = 82; UpdateCamera(); });
        AddButton(toolbar, "Vista isomètrica", (_, _) => { _yaw = -42; _pitch = 42; UpdateCamera(); });
        AddButton(toolbar, "Vista lateral", (_, _) => { _yaw = -90; _pitch = 12; UpdateCamera(); });
        AddButton(toolbar, "Apropar", (_, _) => Zoom(0.8));
        AddButton(toolbar, "Allunyar", (_, _) => Zoom(1.25));
        AddButton(toolbar, "Actualitzar 3D", (_, _) => Render());
        var allLevels = new CheckBox { Content = "Mostra tots els nivells", IsChecked = true, Foreground = Brushes.Gainsboro, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 8, 0) };
        allLevels.Checked += (_, _) => { _showAllLevels = true; Render(); };
        allLevels.Unchecked += (_, _) => { _showAllLevels = false; Render(); };
        toolbar.Children.Add(allLevels);
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(_status);
        _viewport.Camera = _camera;
        _viewport.Children.Add(new ModelVisual3D { Content = _scene });
        _viewport.MouseDown += OnMouseDown;
        _viewport.MouseMove += OnMouseMove;
        _viewport.MouseUp += OnMouseUp;
        _viewport.MouseWheel += OnMouseWheel;
        root.Children.Add(_viewport);
        Content = root;
        UpdateCamera();
        Render();
    }

    public void SetData(IEnumerable<TrackGeometrySegment>? segments, IEnumerable<TrackPiece>? pieces,
        IEnumerable<TrackLevel>? levels, IEnumerable<CatalogueItem>? catalogue, Guid activeLevel)
    {
        _segments = segments?.ToArray() ?? [];
        _pieces = pieces?.ToArray() ?? [];
        _levels = levels?.ToArray() ?? [];
        _catalogue.Clear();
        if (catalogue is not null)
            foreach (var item in catalogue) _catalogue[item.Ref] = item;
        _activeLevel = activeLevel;
        Render();
    }

    public void SetActiveLevel(Guid levelId) { _activeLevel = levelId; Render(); }

    private static void AddButton(Panel panel, string label, RoutedEventHandler click)
    {
        var button = new Button { Content = label, Padding = new Thickness(11, 6, 11, 6), Margin = new Thickness(0, 0, 6, 0) };
        button.Click += click;
        panel.Children.Add(button);
    }

    private void Render()
    {
        _scene.Children.Clear();
        _scene.Children.Add(new AmbientLight(Color.FromRgb(115, 125, 140)));
        _scene.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-0.5, -0.7, -1)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(110, 130, 160), new Vector3D(0.7, 0.2, -0.4)));
        AddGround();
        var level = _levels.FirstOrDefault(x => x.Id == _activeLevel);
        var visibleLevels = _levels
            .Select((item, index) => new { Level = item, Index = index })
            .Where(x => _showAllLevels || x.Level.Id == _activeLevel)
            .ToArray();

        foreach (var entry in visibleLevels)
        {
            var current = entry.Level;
            var elevation = (double)current.ElevationMillimetres;
            var isActive = current.Id == _activeLevel;
            var baseColor = LevelColors[entry.Index % LevelColors.Length];
            var trackColor = isActive ? baseColor : Color.FromRgb(
                (byte)(baseColor.R * 0.58), (byte)(baseColor.G * 0.58), (byte)(baseColor.B * 0.58));

            // Vertical datum marker relates each stored elevation to the ground grid.
            AddCylinder(new Point3D(-1700, -1100, -5), new Point3D(-1700, -1100, elevation + 5),
                isActive ? 4 : 2.5, new DiffuseMaterial(new SolidColorBrush(trackColor)));

            foreach (var segment in _segments.Where(x => x.LevelId == current.Id))
            {
                DrawTrack(BuildSegmentPoints(segment), elevation, trackColor);
                if (segment.Kind == TrackGeometryKind.Turnout) DrawTurnoutBranch(segment, elevation);
            }
            foreach (var piece in _pieces.Where(x => x.LevelId == current.Id))
                DrawPiece(piece, elevation, trackColor);
        }

        var levelSummary = string.Join(" · ", visibleLevels.Select(x =>
            $"{x.Level.Name}: {x.Level.ElevationMillimetres:0.##} mm"));
        var activePieces = _pieces.Count(x => x.LevelId == _activeLevel);
        var activeSegments = _segments.Count(x => x.LevelId == _activeLevel);
        _status.Text = $"Editor 3D natiu · Actiu: {level?.Name ?? "Nivell"} · {activePieces} peces · {activeSegments} traçats · {levelSummary} · Alçades visuals, sense rampes físiques";
    }

    private void AddGround()
    {
        var mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection { new(-1800,-1200,-8), new(1800,-1200,-8), new(1800,1200,-8), new(-1800,1200,-8) },
            TriangleIndices = new Int32Collection { 0,1,2,0,2,3 }
        };
        _scene.Children.Add(new GeometryModel3D(mesh, new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(29, 39, 49)))) { BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(29,39,49))) });
        var gridMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(48, 62, 76)));
        for (var x = -1800; x <= 1800; x += 100) AddCylinder(new Point3D(x, -1200, -5), new Point3D(x, 1200, -5), 0.7, gridMaterial);
        for (var y = -1200; y <= 1200; y += 100) AddCylinder(new Point3D(-1800, y, -5), new Point3D(1800, y, -5), 0.7, gridMaterial);
    }

    private void DrawPiece(TrackPiece piece, double elevation, Color levelColor)
    {
        if (!_catalogue.TryGetValue(piece.CatalogueCode, out var item) || item.TrackPaths.Count == 0)
        {
            AddSphere(new Point3D((double)piece.X, -(double)piece.Y, elevation + 8), 9, Colors.DarkOrange);
            return;
        }
        var ports = item.Ports.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var rotation = (double)piece.RotationDegrees * Math.PI / 180d;
        Point3D Transform(decimal x, decimal y)
        {
            var lx = (double)x; var ly = (double)y;
            var wx = (double)piece.X + lx * Math.Cos(rotation) - ly * Math.Sin(rotation);
            var wy = (double)piece.Y + lx * Math.Sin(rotation) + ly * Math.Cos(rotation);
            return new Point3D(wx, -wy, elevation);
        }
        foreach (var route in item.TrackPaths)
        {
            if (!ports.TryGetValue(route.FromPort, out var from) || !ports.TryGetValue(route.ToPort, out var to)) continue;
            var points = new List<Point3D>();
            var start = Transform(from.X, from.Y);
            var end = Transform(to.X, to.Y);
            if (route.Kind.Equals("quadratic", StringComparison.OrdinalIgnoreCase))
            {
                var control = Transform(route.ControlX, route.ControlY);
                for (var i = 0; i <= 28; i++)
                {
                    var t = i / 28d; var u = 1 - t;
                    points.Add(new Point3D(u*u*start.X + 2*u*t*control.X + t*t*end.X,
                        u*u*start.Y + 2*u*t*control.Y + t*t*end.Y, elevation));
                }
            }
            else { points.Add(start); points.Add(end); }
            DrawTrack(points, elevation, item.GeometryStatus == "confirmed" ? levelColor : Colors.DarkOrange);
        }
    }

    private void DrawTurnoutBranch(TrackGeometrySegment segment, double elevation)
    {
        var a = new Point3D((double)segment.StartX, -(double)segment.StartY, elevation);
        var b = new Point3D((double)segment.EndX, -(double)segment.EndY, elevation);
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var start = new Point3D(a.X + dx * 0.48, a.Y + dy * 0.48, elevation);
        var end = new Point3D(b.X - dx * 0.42 - dy * 0.28, b.Y - dy * 0.42 + dx * 0.28, elevation);
        DrawTrack([start, end], elevation, Colors.DarkOrange);
    }

    private static IReadOnlyList<Point3D> BuildSegmentPoints(TrackGeometrySegment segment)
    {
        var a = new Point3D((double)segment.StartX, -(double)segment.StartY, 0);
        var b = new Point3D((double)segment.EndX, -(double)segment.EndY, 0);
        if (segment.Kind != TrackGeometryKind.Curve) return [a, b];
        var c = new Point3D((double)segment.ControlX, -(double)segment.ControlY, 0);
        var points = new List<Point3D>();
        for (var i = 0; i <= 32; i++)
        {
            var t = i / 32d; var u = 1 - t;
            points.Add(new Point3D(u*u*a.X + 2*u*t*c.X + t*t*b.X, u*u*a.Y + 2*u*t*c.Y + t*t*b.Y, 0));
        }
        return points;
    }

    private void DrawTrack(IReadOnlyList<Point3D> centre, double elevation, Color color)
    {
        if (centre.Count < 2) return;
        var material = new DiffuseMaterial(new SolidColorBrush(color));
        const double gauge = 18;
        for (var i = 1; i < centre.Count; i++)
        {
            var a = centre[i - 1]; var b = centre[i];
            var dx = b.X - a.X; var dy = b.Y - a.Y; var length = Math.Sqrt(dx*dx + dy*dy);
            if (length < 0.001) continue;
            var ox = -dy / length * gauge / 2; var oy = dx / length * gauge / 2;
            AddCylinder(new Point3D(a.X + ox, a.Y + oy, elevation + 8), new Point3D(b.X + ox, b.Y + oy, elevation + 8), 2.3, material);
            AddCylinder(new Point3D(a.X - ox, a.Y - oy, elevation + 8), new Point3D(b.X - ox, b.Y - oy, elevation + 8), 2.3, material);
            AddCylinder(new Point3D(a.X, a.Y, elevation + 3), new Point3D(b.X, b.Y, elevation + 3), 3.8,
                new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(92, 68, 48))));
        }
    }

    private void AddCylinder(Point3D a, Point3D b, double radius, Material material)
    {
        var direction = b - a; var length = direction.Length;
        if (length < 0.001) return;
        direction.Normalize();
        var helper = Math.Abs(Vector3D.DotProduct(direction, new Vector3D(0,0,1))) > 0.95 ? new Vector3D(0,1,0) : new Vector3D(0,0,1);
        var u = Vector3D.CrossProduct(direction, helper); u.Normalize();
        var v = Vector3D.CrossProduct(direction, u); v.Normalize();
        const int sides = 8;
        var mesh = new MeshGeometry3D();
        for (var i = 0; i < sides; i++)
        {
            var angle = 2 * Math.PI * i / sides;
            var offset = u * (Math.Cos(angle) * radius) + v * (Math.Sin(angle) * radius);
            mesh.Positions.Add(a + offset); mesh.Positions.Add(b + offset);
        }
        for (var i = 0; i < sides; i++)
        {
            var n = (i + 1) % sides; var k = i * 2; var m = n * 2;
            mesh.TriangleIndices.Add(k); mesh.TriangleIndices.Add(m); mesh.TriangleIndices.Add(k + 1);
            mesh.TriangleIndices.Add(m); mesh.TriangleIndices.Add(m + 1); mesh.TriangleIndices.Add(k + 1);
        }
        _scene.Children.Add(new GeometryModel3D(mesh, material) { BackMaterial = material });
    }

    private void AddSphere(Point3D centre, double radius, Color color)
    {
        const int rings = 8, slices = 12;
        var mesh = new MeshGeometry3D();
        for (var r = 0; r <= rings; r++)
        {
            var phi = Math.PI * r / rings;
            for (var s = 0; s <= slices; s++)
            {
                var theta = 2 * Math.PI * s / slices;
                mesh.Positions.Add(new Point3D(centre.X + radius * Math.Sin(phi) * Math.Cos(theta),
                    centre.Y + radius * Math.Sin(phi) * Math.Sin(theta), centre.Z + radius * Math.Cos(phi)));
            }
        }
        for (var r = 0; r < rings; r++) for (var s = 0; s < slices; s++)
        {
            var k = r * (slices + 1) + s; var n = k + slices + 1;
            mesh.TriangleIndices.Add(k); mesh.TriangleIndices.Add(n); mesh.TriangleIndices.Add(k+1);
            mesh.TriangleIndices.Add(k+1); mesh.TriangleIndices.Add(n); mesh.TriangleIndices.Add(n+1);
        }
        var material = new DiffuseMaterial(new SolidColorBrush(color));
        _scene.Children.Add(new GeometryModel3D(mesh, material) { BackMaterial = material });
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        _orbiting = true; _lastMouse = e.GetPosition(_viewport); _viewport.CaptureMouse();
    }
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_orbiting || e.LeftButton != MouseButtonState.Pressed) return;
        var p = e.GetPosition(_viewport); var delta = p - _lastMouse; _lastMouse = p;
        _yaw += delta.X * 0.45; _pitch = Math.Clamp(_pitch - delta.Y * 0.35, 5, 88); UpdateCamera();
    }
    private void OnMouseUp(object sender, MouseButtonEventArgs e) { _orbiting = false; _viewport.ReleaseMouseCapture(); }
    private void OnMouseWheel(object sender, MouseWheelEventArgs e) => Zoom(e.Delta > 0 ? 0.85 : 1.18);
    private void Zoom(double factor) { _distance = Math.Clamp(_distance * factor, 250, 9000); UpdateCamera(); }
    private void UpdateCamera()
    {
        var yaw = _yaw * Math.PI / 180; var pitch = _pitch * Math.PI / 180;
        var target = new Point3D(0, 0, 0);
        var x = _distance * Math.Cos(pitch) * Math.Sin(yaw);
        var y = -_distance * Math.Cos(pitch) * Math.Cos(yaw);
        var z = _distance * Math.Sin(pitch);
        _camera.Position = new Point3D(target.X + x, target.Y + y, target.Z + z);
        _camera.LookDirection = target - _camera.Position;
        _camera.UpDirection = new Vector3D(0, 0, 1);
    }
}
