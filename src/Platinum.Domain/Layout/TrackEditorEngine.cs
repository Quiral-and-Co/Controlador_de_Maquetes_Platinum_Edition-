namespace Platinum.Domain.Layout;

/// <summary>
/// Platform-independent editing operations shared by WPF and Avalonia.
/// Coordinates and tolerances are millimetres; this class performs no rendering or I/O.
/// </summary>
public sealed class TrackEditorEngine
{
    private sealed record Snapshot(
        TrackGeometrySegment[] Segments,
        TrackPiece[] Pieces,
        TrackConnection[] Connections);

    private readonly List<TrackGeometrySegment> _segments = [];
    private readonly List<TrackPiece> _pieces = [];
    private readonly List<TrackConnection> _connections = [];
    private readonly Stack<Snapshot> _undo = new();
    private readonly Stack<Snapshot> _redo = new();

    public IReadOnlyList<TrackGeometrySegment> Segments => _segments;
    public IReadOnlyList<TrackPiece> Pieces => _pieces;
    public IReadOnlyList<TrackConnection> Connections => _connections;

    public void Load(IEnumerable<TrackGeometrySegment>? segments,
        IEnumerable<TrackPiece>? pieces, IEnumerable<TrackConnection>? connections)
    {
        _segments.Clear();
        _pieces.Clear();
        _connections.Clear();
        if (segments is not null) _segments.AddRange(segments);
        if (pieces is not null) _pieces.AddRange(pieces);
        if (connections is not null) _connections.AddRange(connections);
        _undo.Clear();
        _redo.Clear();
    }

    public static decimal Snap(decimal value, decimal gridMillimetres, bool enabled = true)
    {
        if (gridMillimetres <= 0m || gridMillimetres > 1000m)
            throw new ArgumentOutOfRangeException(nameof(gridMillimetres), "La graella ha d'estar entre 0 i 1000 mm.");
        return enabled ? decimal.Round(value / gridMillimetres, 0, MidpointRounding.ToEven) * gridMillimetres : value;
    }

    public static (decimal X, decimal Y) Snap(decimal x, decimal y, decimal gridMillimetres, bool enabled = true) =>
        (Snap(x, gridMillimetres, enabled), Snap(y, gridMillimetres, enabled));

    /// <summary>Creates the same straight/quadratic/turnout centre-line as the original WPF editor.</summary>
    public TrackGeometrySegment AddSegment(TrackGeometryKind kind,
        decimal startX, decimal startY, decimal endX, decimal endY, Guid levelId)
    {
        if (startX == endX && startY == endY)
            throw new ArgumentException("El segment ha de tenir longitud positiva.");
        var dx = endX - startX;
        var dy = endY - startY;
        var segment = new TrackGeometrySegment(Guid.NewGuid(), kind, startX, startY, endX, endY,
            startX + dx / 2m - dy * 0.22m,
            startY + dy / 2m + dx * 0.22m, levelId);
        PushUndo();
        _segments.Add(segment);
        return segment;
    }

    public bool MoveSegment(Guid segmentId, decimal deltaX, decimal deltaY)
    {
        var index = _segments.FindIndex(x => x.Id == segmentId);
        if (index < 0) return false;
        if (deltaX == 0m && deltaY == 0m) return false;
        PushUndo();
        var s = _segments[index];
        _segments[index] = s with
        {
            StartX = s.StartX + deltaX, StartY = s.StartY + deltaY,
            EndX = s.EndX + deltaX, EndY = s.EndY + deltaY,
            ControlX = s.ControlX + deltaX, ControlY = s.ControlY + deltaY
        };
        return true;
    }

    public bool MovePiece(Guid pieceId, decimal deltaX, decimal deltaY)
    {
        var index = _pieces.FindIndex(x => x.Id == pieceId);
        if (index < 0) return false;
        if (deltaX == 0m && deltaY == 0m) return false;
        PushUndo();
        var piece = _pieces[index];
        _pieces[index] = piece with { X = piece.X + deltaX, Y = piece.Y + deltaY };
        return true;
    }

    public bool RotatePiece(Guid pieceId, decimal deltaDegrees)
    {
        var index = _pieces.FindIndex(x => x.Id == pieceId);
        if (index < 0) return false;
        if (deltaDegrees == 0m) return false;
        PushUndo();
        var piece = _pieces[index];
        _pieces[index] = piece with
        {
            RotationDegrees = ((piece.RotationDegrees + deltaDegrees) % 360m + 360m) % 360m
        };
        return true;
    }

    public bool TryConnect(Guid fromPieceId, string fromPort, Guid toPieceId, string toPort,
        out TrackConnection? connection)
    {
        connection = null;
        if (fromPieceId == toPieceId) return false;
        if (!_pieces.Any(x => x.Id == fromPieceId) || !_pieces.Any(x => x.Id == toPieceId)) return false;
        if (string.IsNullOrWhiteSpace(fromPort) || string.IsNullOrWhiteSpace(toPort)) return false;
        var exists = _connections.Any(x =>
            (x.FromPieceId == fromPieceId && x.FromPort == fromPort && x.ToPieceId == toPieceId && x.ToPort == toPort) ||
            (x.FromPieceId == toPieceId && x.FromPort == toPort && x.ToPieceId == fromPieceId && x.ToPort == fromPort));
        if (exists) return false;
        PushUndo();
        connection = new TrackConnection(Guid.NewGuid(), fromPieceId, fromPort, toPieceId, toPort, false);
        _connections.Add(connection);
        return true;
    }

    public static (decimal X, decimal Y) TransformPort(TrackPiece piece, decimal localX, decimal localY)
    {
        var radians = (double)piece.RotationDegrees * Math.PI / 180d;
        return (
            piece.X + (decimal)((double)localX * Math.Cos(radians) - (double)localY * Math.Sin(radians)),
            piece.Y + (decimal)((double)localX * Math.Sin(radians) + (double)localY * Math.Cos(radians)));
    }

    public Guid? HitTestPiece(decimal x, decimal y, Guid levelId, decimal toleranceMm = 115m)
    {
        foreach (var piece in _pieces.Where(p => p.LevelId == levelId).Reverse())
            if (Distance(x, y, piece.X, piece.Y) <= toleranceMm) return piece.Id;
        return null;
    }

    public Guid? HitTestSegment(decimal x, decimal y, Guid levelId, decimal toleranceMm = 55m)
    {
        foreach (var s in _segments.Where(s => s.LevelId == levelId).Reverse())
        {
            if (DistanceToSegment(x, y, s.StartX, s.StartY, s.EndX, s.EndY) <= toleranceMm)
                return s.Id;
            if (s.Kind == TrackGeometryKind.Curve &&
                (DistanceToSegment(x, y, s.StartX, s.StartY, s.ControlX, s.ControlY) <= toleranceMm ||
                 DistanceToSegment(x, y, s.ControlX, s.ControlY, s.EndX, s.EndY) <= toleranceMm))
                return s.Id;
        }
        return null;
    }

    public (Guid PieceId, string PortId)? HitTestPort(decimal x, decimal y, Guid levelId,
        IEnumerable<TrackEditorPort> ports, decimal toleranceMm = 22m)
    {
        var byCode = ports.GroupBy(p => p.CatalogueCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
        (Guid PieceId, string PortId)? nearest = null;
        var nearestDistance = toleranceMm;
        foreach (var piece in _pieces.Where(p => p.LevelId == levelId).Reverse())
        {
            if (!byCode.TryGetValue(piece.CatalogueCode, out var piecePorts)) continue;
            foreach (var port in piecePorts)
            {
                var world = TransformPort(piece, port.X, port.Y);
                var distance = Distance(x, y, world.X, world.Y);
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = (piece.Id, port.Id);
                }
            }
        }
        return nearest;
    }

    public bool Undo()
    {
        if (_undo.Count == 0) return false;
        _redo.Push(Capture());
        Restore(_undo.Pop());
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0) return false;
        _undo.Push(Capture());
        Restore(_redo.Pop());
        return true;
    }

    private void PushUndo()
    {
        _undo.Push(Capture());
        _redo.Clear();
    }

    private Snapshot Capture() => new(_segments.ToArray(), _pieces.ToArray(), _connections.ToArray());

    private void Restore(Snapshot snapshot)
    {
        _segments.Clear(); _segments.AddRange(snapshot.Segments);
        _pieces.Clear(); _pieces.AddRange(snapshot.Pieces);
        _connections.Clear(); _connections.AddRange(snapshot.Connections);
    }

    private static decimal Distance(decimal x1, decimal y1, decimal x2, decimal y2)
    {
        var dx = (double)(x1 - x2);
        var dy = (double)(y1 - y2);
        return (decimal)Math.Sqrt(dx * dx + dy * dy);
    }

    private static decimal DistanceToSegment(decimal px, decimal py, decimal ax, decimal ay, decimal bx, decimal by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        if (dx == 0m && dy == 0m) return Distance(px, py, ax, ay);
        var t = Math.Clamp(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy), 0m, 1m);
        return Distance(px, py, ax + t * dx, ay + t * dy);
    }
}

/// <summary>Catalogue port geometry supplied by the presentation adapter, in local millimetres.</summary>
public sealed record TrackEditorPort(string CatalogueCode, string Id, decimal X, decimal Y, string Accuracy = "derived");
