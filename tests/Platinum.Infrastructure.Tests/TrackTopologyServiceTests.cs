using Platinum.Application.Layout;
using Platinum.Domain.Layout;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class TrackTopologyServiceTests
{
    private static (TrackLayout Layout, Guid Level, Guid First, Guid Second) CreateLayout()
    {
        var level = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var layout = new TrackLayout
        {
            Levels = [new TrackLevel(level, "Principal", 0m)],
            Pieces =
            [
                new TrackPiece(first, "A", 0m, 0m, 0m, level),
                new TrackPiece(second, "B", 100m, 0m, 0m, level)
            ]
        };
        return (layout, level, first, second);
    }

    [Fact]
    public void Connect_AddsConnection_WhenPortsAreWithinTolerance()
    {
        var (layout, _, first, second) = CreateLayout();
        var result = new TrackTopologyService().Connect(layout, first, "OUT", 100m, 0m, second, "IN", 102m, 0m);

        Assert.True(result.Success);
        Assert.NotNull(result.Connection);
        Assert.Equal(2m, result.DistanceMillimetres);
        Assert.Single(layout.Connections);
        Assert.False(layout.Connections[0].PhysicallyVerified);
    }

    [Fact]
    public void Connect_RejectsPortsOutsideTolerance()
    {
        var (layout, _, first, second) = CreateLayout();
        var result = new TrackTopologyService().Connect(layout, first, "OUT", 100m, 0m, second, "IN", 110m, 0m);

        Assert.False(result.Success);
        Assert.Empty(layout.Connections);
    }

    [Fact]
    public void Connect_RejectsAlreadyOccupiedPort()
    {
        var (layout, _, first, second) = CreateLayout();
        layout.Connections.Add(new TrackConnection(Guid.NewGuid(), first, "OUT", Guid.NewGuid(), "IN"));
        var result = new TrackTopologyService().Connect(layout, first, "OUT", 0m, 0m, second, "IN", 0m, 0m);

        Assert.False(result.Success);
        Assert.Empty(layout.Connections.Skip(1));
    }

    [Fact]
    public void Connect_RejectsDifferentLevels()
    {
        var (layout, _, first, second) = CreateLayout();
        var otherLevel = Guid.NewGuid();
        layout.Levels.Add(new TrackLevel(otherLevel, "Superior", 110m));
        var index = layout.Pieces.FindIndex(piece => piece.Id == second);
        layout.Pieces[index] = layout.Pieces[index] with { LevelId = otherLevel };

        var result = new TrackTopologyService().Connect(layout, first, "OUT", 0m, 0m, second, "IN", 0m, 0m);

        Assert.False(result.Success);
        Assert.Empty(layout.Connections);
    }

    [Fact]
    public void Connect_RejectsSamePiece()
    {
        var (layout, _, first, _) = CreateLayout();
        var result = new TrackTopologyService().Connect(layout, first, "A", 0m, 0m, first, "B", 0m, 0m);

        Assert.False(result.Success);
        Assert.Empty(layout.Connections);
    }
}
