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
    public List<SignalDefinition> Signals { get; init; } = [];
    public List<LocomotiveProfile> Locomotives { get; init; } = [];
    public List<RailwayProtocolProfile> ProtocolProfiles { get; init; } = [];
    public List<PhysicalInspectionRecord> Inspections { get; init; } = [];
    public List<TrainProtectionProfile> ProtectionProfiles { get; init; } = [];
    public List<TrainGroundMessage> GroundMessages { get; init; } = [];
    public decimal GameFuelPercent { get; set; } = 100m;
}

public sealed record TrackLevel(Guid Id, string Name, decimal ElevationMillimetres);
public sealed record TrackPiece(Guid Id, string CatalogueCode, decimal X, decimal Y, decimal RotationDegrees, Guid LevelId, string GeometryStatus = "unverified");
public sealed record TrackConnection(Guid Id, Guid FromPieceId, string FromPort, Guid ToPieceId, string ToPort, bool PhysicallyVerified = false);
public sealed record LogicalBlock(Guid Id, string Name, Guid LevelId, IReadOnlyList<Guid> PieceIds, bool Occupied = false);
public sealed record FeedbackSensor(Guid Id, string SystemName, string DisplayName, bool PhysicallyVerified = false);
public sealed record RouteDefinition(Guid Id, string Name, IReadOnlyList<Guid> BlockIds, bool Reserved = false);

public sealed record SignalDefinition(Guid Id, string Name, string Aspect, Guid? ProtectedBlockId, bool PhysicallyVerified = false);
public sealed record LocomotiveProfile(Guid Id, string Name, int Address, string Protocol, bool SimulationOnly = true);
public sealed record RailwayProtocolProfile(Guid Id, string Name, string Kind, string Host, int Port, bool PhysicalCommandsEnabled = false);
public sealed record PhysicalInspectionRecord(Guid Id, string Item, bool OperatorDeclaredComplete, string Notes, DateTimeOffset CheckedUtc);
public sealed record TrainProtectionProfile(Guid Id, string Name, decimal PermittedSpeedKmh, decimal MaxSpeedKmh, decimal ServiceBrakeDeceleration, bool SimulationOnly = true);
public sealed record TrainGroundMessage(Guid Id, string Destination, string Message, DateTimeOffset CreatedUtc);
