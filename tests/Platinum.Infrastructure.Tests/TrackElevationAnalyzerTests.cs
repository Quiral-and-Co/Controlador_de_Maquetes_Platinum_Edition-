using Platinum.Domain.Layout;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class TrackElevationAnalyzerTests
{
    [Fact]
    public void CalculatesGradeBetweenAdjacentLevelsInElevationOrder()
    {
        var high = new TrackLevel(Guid.NewGuid(), "Superior", 220m);
        var low = new TrackLevel(Guid.NewGuid(), "Inferior", 0m);
        var middle = new TrackLevel(Guid.NewGuid(), "Intermedi", 110m);

        var results = TrackElevationAnalyzer.AnalyzeAdjacentLevels(
            [high, low, middle], 1000m);

        Assert.Equal(2, results.Count);
        Assert.Equal("Inferior", results[0].LowerLevelName);
        Assert.Equal("Intermedi", results[0].UpperLevelName);
        Assert.Equal(110m, results[0].RiseMillimetres);
        Assert.Equal(11m, results[0].GradePercent);
        Assert.Equal("Intermedi", results[1].LowerLevelName);
        Assert.Equal("Superior", results[1].UpperLevelName);
    }

    [Fact]
    public void RejectsZeroOrNegativeHorizontalRun()
    {
        var levels = new[]
        {
            new TrackLevel(Guid.NewGuid(), "Baix", 0m),
            new TrackLevel(Guid.NewGuid(), "Alt", 100m)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TrackElevationAnalyzer.AnalyzeAdjacentLevels(levels, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TrackElevationAnalyzer.AnalyzeAdjacentLevels(levels, -100m));
    }

    [Fact]
    public void RequiresAtLeastTwoLevels()
    {
        var level = new TrackLevel(Guid.NewGuid(), "Únic", 0m);
        Assert.Throws<ArgumentException>(() =>
            TrackElevationAnalyzer.AnalyzeAdjacentLevels([level], 1000m));
    }
}
