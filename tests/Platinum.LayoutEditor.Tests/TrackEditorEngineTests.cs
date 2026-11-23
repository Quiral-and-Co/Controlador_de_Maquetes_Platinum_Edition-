using Xunit;
using Platinum.Domain.Layout;

namespace Platinum.LayoutEditor.Tests;

public sealed class TrackEditorEngineTests
{
    private static readonly Guid Level = Guid.NewGuid();

    [Fact]
    public void AddCurve_UsesOriginalWpfControlPointFormula_AndStoresMillimetres()
    {
        var engine = new TrackEditorEngine();
        var segment = engine.AddSegment(TrackGeometryKind.Curve, 100m, 200m, 500m, 600m, Level);
        Assert.Equal(212m, segment.ControlX);
        Assert.Equal(488m, segment.ControlY);
        Assert.Equal(100m, segment.StartX);
        Assert.Equal(600m, segment.EndY);
    }

    [Fact]
    public void MoveSegment_TranslatesBothEndpointsAndControlPoint()
    {
        var engine = new TrackEditorEngine();
        var segment = engine.AddSegment(TrackGeometryKind.Curve, 10m, 20m, 110m, 120m, Level);
        Assert.True(engine.MoveSegment(segment.Id, 30m, -5m));
        var moved = Assert.Single(engine.Segments);
        Assert.Equal(40m, moved.StartX);
        Assert.Equal(50m, moved.StartY);
        Assert.Equal(140m, moved.EndX);
        Assert.Equal(115m, moved.EndY);
        Assert.Equal(segment.ControlX + 30m, moved.ControlX);
        Assert.Equal(segment.ControlY - 5m, moved.ControlY);
    }

    [Fact]
    public void RotatePiece_NormalizesAngleAndUndoRedoRestoresExactValue()
    {
        var engine = new TrackEditorEngine();
        var piece = new TrackPiece(Guid.NewGuid(), "24188", 250m, 300m, 350m, Level);
        engine.Load(null, [piece], null);
        Assert.True(engine.RotatePiece(piece.Id, 15m));
        Assert.Equal(5m, Assert.Single(engine.Pieces).RotationDegrees);
        Assert.True(engine.Undo());
        Assert.Equal(350m, Assert.Single(engine.Pieces).RotationDegrees);
        Assert.True(engine.Redo());
        Assert.Equal(5m, Assert.Single(engine.Pieces).RotationDegrees);
    }

    [Fact]
    public void Snap_UsesGridInMillimetres_AndCanBeDisabled()
    {
        Assert.Equal((20m, 30m), TrackEditorEngine.Snap(24m, 26m, 10m));
        Assert.Equal((24m, 26m), TrackEditorEngine.Snap(24m, 26m, 10m, false));
    }

    [Fact]
    public void HitTesting_UsesExistingPieceAndSegmentTolerances()
    {
        var engine = new TrackEditorEngine();
        var piece = new TrackPiece(Guid.NewGuid(), "24188", 100m, 100m, 0m, Level);
        engine.Load([new TrackGeometrySegment(Guid.NewGuid(), TrackGeometryKind.Straight, 0m, 0m, 200m, 0m, 100m, 0m, Level)], [piece], null);
        Assert.Equal(piece.Id, engine.HitTestPiece(200m, 100m, Level));
        Assert.NotNull(engine.HitTestSegment(100m, 40m, Level));
        Assert.Null(engine.HitTestSegment(100m, 80m, Level));
    }

    [Fact]
    public void Connections_AreLogicalOnly_RejectDuplicatesAndCanBeUndone()
    {
        var engine = new TrackEditorEngine();
        var a = new TrackPiece(Guid.NewGuid(), "24188", 100m, 100m, 0m, Level);
        var b = new TrackPiece(Guid.NewGuid(), "24188", 300m, 100m, 0m, Level);
        engine.Load(null, [a, b], null);
        Assert.True(engine.TryConnect(a.Id, "A", b.Id, "B", out var connection));
        Assert.False(connection!.PhysicallyVerified);
        Assert.False(engine.TryConnect(b.Id, "B", a.Id, "A", out _));
        Assert.Single(engine.Connections);
        Assert.True(engine.Undo());
        Assert.Empty(engine.Connections);
        Assert.True(engine.Redo());
        Assert.Single(engine.Connections);
    }

    [Fact]
    public void PortTransformAndHitTesting_RotateLocalCoordinatesIntoWorldSpace()
    {
        var engine = new TrackEditorEngine();
        var piece = new TrackPiece(Guid.NewGuid(), "X", 100m, 200m, 90m, Level);
        engine.Load(null, [piece], null);
        var world = TrackEditorEngine.TransformPort(piece, 10m, 0m);
        Assert.Equal(100m, world.X);
        Assert.Equal(210m, world.Y);
        var hit = engine.HitTestPort(100m, 210m, Level, [new TrackEditorPort("X", "A", 10m, 0m)]);
        Assert.Equal((piece.Id, "A"), hit);
    }
}