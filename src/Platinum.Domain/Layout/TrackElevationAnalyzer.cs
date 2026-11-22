namespace Platinum.Domain.Layout;

/// <summary>
/// Calculates theoretical grades between adjacent layout levels.
/// This is a geometric estimate only; it does not certify ramp length,
/// rolling stock clearance, traction, or physical construction.
/// </summary>
public static class TrackElevationAnalyzer
{
    public static IReadOnlyList<TrackGradeResult> AnalyzeAdjacentLevels(
        IEnumerable<TrackLevel> levels,
        decimal horizontalRunMillimetres)
    {
        ArgumentNullException.ThrowIfNull(levels);
        if (horizontalRunMillimetres <= 0)
            throw new ArgumentOutOfRangeException(nameof(horizontalRunMillimetres),
                "La distància horitzontal ha de ser superior a zero.");

        var ordered = levels.OrderBy(level => level.ElevationMillimetres).ToArray();
        if (ordered.Length < 2)
            throw new ArgumentException("Calen com a mínim dos nivells per calcular un pendent.", nameof(levels));

        var results = new List<TrackGradeResult>(ordered.Length - 1);
        for (var index = 0; index < ordered.Length - 1; index++)
        {
            var lower = ordered[index];
            var upper = ordered[index + 1];
            var rise = upper.ElevationMillimetres - lower.ElevationMillimetres;
            results.Add(new TrackGradeResult(
                lower.Id,
                lower.Name,
                upper.Id,
                upper.Name,
                rise,
                horizontalRunMillimetres,
                rise / horizontalRunMillimetres * 100m));
        }

        return results;
    }
}

public sealed record TrackGradeResult(
    Guid LowerLevelId,
    string LowerLevelName,
    Guid UpperLevelId,
    string UpperLevelName,
    decimal RiseMillimetres,
    decimal HorizontalRunMillimetres,
    decimal GradePercent);
