using Platinum.Domain.Layout;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class TrackTopologyValidatorTests
{
    private static TrackLayout Layout()
    {
        var level = new TrackLevel(Guid.NewGuid(), "Nivell 1", 0);
        var a = new TrackPiece(Guid.NewGuid(), "straight:C1", 100, 100, 0, level.Id);
        var b = new TrackPiece(Guid.NewGuid(), "straight:C1", 200, 100, 0, level.Id);
        return new TrackLayout
        {
            WidthMillimetres = 3000,
            HeightMillimetres = 1800,
            Levels = [level],
            Pieces = [a, b],
            Connections = [new TrackConnection(Guid.NewGuid(), a.Id, "B", b.Id, "A")]
        };
    }

    [Fact]
    public void AcceptsValidLogicalConnectionWithoutClaimingPhysicalVerification()
    {
        var layout = Layout();
        var issues = TrackTopologyValidator.Validate(layout);
        Assert.Empty(issues);
        Assert.All(layout.Connections, connection => Assert.False(connection.PhysicallyVerified));
    }

    [Fact]
    public void DetectsPortReuse()
    {
        var layout = Layout();
        var first = layout.Connections[0];
        layout.Connections.Add(new TrackConnection(Guid.NewGuid(), first.FromPieceId, "B",
            first.ToPieceId, "C"));
        var issues = TrackTopologyValidator.Validate(layout);
        Assert.Contains(issues, issue => issue.Code == "CONNECTION_PIECE_MISSING");
        Assert.Contains(issues, issue => issue.Code == "PORT_ALREADY_CONNECTED");
    }

    [Fact]
    public void DetectsCrossLevelConnections()
    {
        var layout = Layout();
        var secondLevel = new TrackLevel(Guid.NewGuid(), "Nivell 2", 110);
        layout.Levels.Add(secondLevel);
        layout.Pieces[1] = layout.Pieces[1] with { LevelId = secondLevel.Id };
        var issues = TrackTopologyValidator.Validate(layout);
        Assert.Contains(issues, issue => issue.Code == "CONNECTION_CROSS_LEVEL");
    }

    [Fact]
    public void DetectsGeometryOutsideRoomAndZeroLength()
    {
        var layout = Layout();
        var levelId = layout.Levels[0].Id;
        layout.GeometrySegments.Add(new TrackGeometrySegment(Guid.NewGuid(), TrackGeometryKind.Straight,
            3100, 100, 3100, 100, 3100, 100, levelId));
        var issues = TrackTopologyValidator.Validate(layout);
        Assert.Contains(issues, issue => issue.Code == "SEGMENT_ZERO_LENGTH");
        Assert.Contains(issues, issue => issue.Code == "SEGMENT_OUT_OF_BOUNDS");
    }
}
