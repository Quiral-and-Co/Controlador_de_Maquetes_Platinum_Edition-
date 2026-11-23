using Platinum.Domain.Layout;

namespace Platinum.Application.Layout;

/// <summary>Validates and records physical track-port connections in layout coordinates (millimetres).</summary>
public sealed class TrackTopologyService
{
    public const decimal DefaultPortToleranceMillimetres = 3m;

    public TrackConnectionResult Connect(
        TrackLayout layout,
        Guid fromPieceId,
        string fromPort,
        decimal fromX,
        decimal fromY,
        Guid toPieceId,
        string toPort,
        decimal toX,
        decimal toY,
        decimal toleranceMillimetres = DefaultPortToleranceMillimetres)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (toleranceMillimetres < 0m)
            return TrackConnectionResult.Rejected("La tolerància no pot ser negativa.");
        if (string.IsNullOrWhiteSpace(fromPort) || string.IsNullOrWhiteSpace(toPort))
            return TrackConnectionResult.Rejected("Cal indicar els dos ports.");
        if (fromPieceId == toPieceId)
            return TrackConnectionResult.Rejected("No es pot connectar una peça amb ella mateixa.");

        var from = layout.Pieces.FirstOrDefault(piece => piece.Id == fromPieceId);
        var to = layout.Pieces.FirstOrDefault(piece => piece.Id == toPieceId);
        if (from is null || to is null)
            return TrackConnectionResult.Rejected("Una de les peces ja no existeix al plànol.");
        if (from.LevelId != to.LevelId)
            return TrackConnectionResult.Rejected("No es poden connectar ports de nivells diferents.");

        var dx = fromX - toX;
        var dy = fromY - toY;
        var distance = (decimal)Math.Sqrt((double)(dx * dx + dy * dy));
        if (distance > toleranceMillimetres)
            return TrackConnectionResult.Rejected($"Els ports estan separats {distance:0.##} mm; el màxim admès és {toleranceMillimetres:0.##} mm.");

        var fromOccupied = layout.Connections.Any(connection =>
            (connection.FromPieceId == fromPieceId && connection.FromPort.Equals(fromPort, StringComparison.OrdinalIgnoreCase)) ||
            (connection.ToPieceId == fromPieceId && connection.ToPort.Equals(fromPort, StringComparison.OrdinalIgnoreCase)));
        var toOccupied = layout.Connections.Any(connection =>
            (connection.FromPieceId == toPieceId && connection.FromPort.Equals(toPort, StringComparison.OrdinalIgnoreCase)) ||
            (connection.ToPieceId == toPieceId && connection.ToPort.Equals(toPort, StringComparison.OrdinalIgnoreCase)));
        if (fromOccupied || toOccupied)
            return TrackConnectionResult.Rejected("Un dels ports ja està connectat.");

        var connection = new TrackConnection(Guid.NewGuid(), fromPieceId, fromPort, toPieceId, toPort, false);
        layout.Connections.Add(connection);
        return TrackConnectionResult.Accepted(connection, distance);
    }
}

public sealed record TrackConnectionResult(bool Success, string Message, TrackConnection? Connection, decimal DistanceMillimetres)
{
    public static TrackConnectionResult Accepted(TrackConnection connection, decimal distance) =>
        new(true, "Connexió topològica creada; verificació física pendent.", connection, distance);

    public static TrackConnectionResult Rejected(string message) => new(false, message, null, 0m);
}
