namespace Platinum.Domain.Layout;

public sealed class TrackLayout
{
    public const int CurrentFormatVersion = 1;
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "Nova maqueta";
    public decimal WidthMillimetres { get; set; } = 3000m;
    public decimal HeightMillimetres { get; set; } = 1800m;
    public decimal GridMillimetres { get; set; } = 10m;
    public List<TrackLevel> Levels { get; init; } = [];
    public List<TrackPiece> Pieces { get; init; } = [];
    public List<TrackGeometrySegment> GeometrySegments { get; set; } = [];
    public List<TrackConnection> Connections { get; init; } = [];
    public List<LogicalBlock> Blocks { get; init; } = [];
    public List<FeedbackSensor> Sensors { get; init; } = [];
    public List<RouteDefinition> Routes { get; init; } = [];
}

public sealed record TrackLevel(Guid Id, string Name, decimal ElevationMillimetres);
public sealed record TrackPiece(Guid Id, string CatalogueCode, decimal X, decimal Y, decimal RotationDegrees, Guid LevelId, string GeometryStatus = "unverified");
public sealed record TrackConnection(Guid Id, Guid FromPieceId, string FromPort, Guid ToPieceId, string ToPort, bool PhysicallyVerified = false);
public sealed record LogicalBlock(Guid Id, string Name, Guid LevelId, IReadOnlyList<Guid> PieceIds);
public sealed record FeedbackSensor(Guid Id, string SystemName, string DisplayName, bool PhysicallyVerified = false);
public sealed record RouteDefinition(Guid Id, string Name, IReadOnlyList<Guid> BlockIds);
