namespace Platinum.Domain.Layout;

/// <summary>
/// Editable centre-line geometry in millimetres. ControlPoint is used by quadratic curves;
/// turnout geometry uses the same endpoints plus a deterministic branch angle.
/// </summary>
public sealed record TrackGeometrySegment(
    Guid Id,
    TrackGeometryKind Kind,
    decimal StartX,
    decimal StartY,
    decimal EndX,
    decimal EndY,
    decimal ControlX,
    decimal ControlY,
    Guid LevelId);

public enum TrackGeometryKind
{
    Straight = 0,
    Curve = 1,
    Turnout = 2
}
