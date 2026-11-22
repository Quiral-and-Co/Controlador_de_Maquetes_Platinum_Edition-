namespace Platinum.Domain.Layout;

/// <summary>
/// Validates logical track topology only. A successful result is not a physical
/// fit, electrical, safety, or hardware certification.
/// </summary>
public static class TrackTopologyValidator
{
    private static readonly HashSet<string> ValidPorts = new(StringComparer.Ordinal) { "A", "B", "C" };

    public static IReadOnlyList<TopologyIssue> Validate(TrackLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var issues = new List<TopologyIssue>();
        var levelIds = layout.Levels.Select(x => x.Id).ToHashSet();
        var pieceIds = layout.Pieces.Select(x => x.Id).ToHashSet();

        AddDuplicateIssues(layout.Levels.Select(x => x.Id), "LEVEL_ID_DUPLICATE", "Hi ha identificadors de nivell duplicats.", issues);
        AddDuplicateIssues(layout.Pieces.Select(x => x.Id), "PIECE_ID_DUPLICATE", "Hi ha identificadors de peça duplicats.", issues);
        AddDuplicateIssues(layout.GeometrySegments.Select(x => x.Id), "SEGMENT_ID_DUPLICATE", "Hi ha identificadors de segment duplicats.", issues);
        AddDuplicateIssues(layout.Connections.Select(x => x.Id), "CONNECTION_ID_DUPLICATE", "Hi ha identificadors de connexió duplicats.", issues);

        if (layout.WidthMillimetres <= 0 || layout.HeightMillimetres <= 0)
            issues.Add(new("ROOM_DIMENSIONS_INVALID", "Les dimensions de la maqueta han de ser positives."));

        foreach (var piece in layout.Pieces)
        {
            if (!levelIds.Contains(piece.LevelId))
                issues.Add(new("PIECE_LEVEL_MISSING", $"La peça {piece.Id} referencia un nivell inexistent."));
            if (piece.X < 0 || piece.X > layout.WidthMillimetres || piece.Y < 0 || piece.Y > layout.HeightMillimetres)
                issues.Add(new("PIECE_OUT_OF_BOUNDS", $"La peça {piece.Id} queda fora dels límits de la maqueta."));
        }

        foreach (var segment in layout.GeometrySegments)
        {
            if (!levelIds.Contains(segment.LevelId))
                issues.Add(new("SEGMENT_LEVEL_MISSING", $"El segment {segment.Id} referencia un nivell inexistent."));
            if (segment.StartX == segment.EndX && segment.StartY == segment.EndY)
                issues.Add(new("SEGMENT_ZERO_LENGTH", $"El segment {segment.Id} té longitud zero."));
            if (!Inside(segment.StartX, segment.StartY, layout) || !Inside(segment.EndX, segment.EndY, layout))
                issues.Add(new("SEGMENT_OUT_OF_BOUNDS", $"El segment {segment.Id} surt dels límits de la maqueta."));
        }

        var occupiedPorts = new HashSet<string>(StringComparer.Ordinal);
        var seenEdges = new HashSet<string>(StringComparer.Ordinal);
        foreach (var connection in layout.Connections)
        {
            if (!pieceIds.Contains(connection.FromPieceId) || !pieceIds.Contains(connection.ToPieceId))
            {
                issues.Add(new("CONNECTION_PIECE_MISSING", $"La connexió {connection.Id} referencia una peça inexistent."));
                continue;
            }
            if (connection.FromPieceId == connection.ToPieceId)
                issues.Add(new("CONNECTION_SELF_LINK", $"La connexió {connection.Id} uneix una peça amb ella mateixa."));
            if (!ValidPorts.Contains(connection.FromPort) || !ValidPorts.Contains(connection.ToPort))
                issues.Add(new("CONNECTION_PORT_INVALID", $"La connexió {connection.Id} conté un port no vàlid."));
            var from = layout.Pieces.First(x => x.Id == connection.FromPieceId);
            var to = layout.Pieces.First(x => x.Id == connection.ToPieceId);
            if (from.LevelId != to.LevelId)
                issues.Add(new("CONNECTION_CROSS_LEVEL", $"La connexió {connection.Id} uneix peces de nivells diferents."));

            var a = $"{connection.FromPieceId:N}:{connection.FromPort}";
            var b = $"{connection.ToPieceId:N}:{connection.ToPort}";
            if (!occupiedPorts.Add(a) || !occupiedPorts.Add(b))
                issues.Add(new("PORT_ALREADY_CONNECTED", $"La connexió {connection.Id} reutilitza un port ja ocupat."));
            var edge = string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
            if (!seenEdges.Add(edge))
                issues.Add(new("CONNECTION_DUPLICATE", $"La connexió {connection.Id} està duplicada."));
        }
        return issues;
    }

    private static bool Inside(decimal x, decimal y, TrackLayout layout) =>
        x >= 0 && x <= layout.WidthMillimetres && y >= 0 && y <= layout.HeightMillimetres;

    private static void AddDuplicateIssues(IEnumerable<Guid> ids, string code, string message, List<TopologyIssue> issues)
    {
        if (ids.GroupBy(x => x).Any(group => group.Count() > 1))
            issues.Add(new(code, message));
    }
}

public sealed record TopologyIssue(string Code, string Message);
