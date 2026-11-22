using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Platinum.Domain.Layout;

namespace Platinum.Infrastructure.Projects;

/// <summary>Imports the legacy JSON layout v3 format into the native domain model.
/// Imported geometry and hardware remain unverified; protocol outputs stay disabled.</summary>
public sealed class JsonV3LayoutImporter
{
    public TrackLayout Import(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("schemaVersion", out var version) || version.GetInt32() != 3)
            throw new InvalidDataException("Només s'admeten projectes JSON schemaVersion 3.");
        if (!root.TryGetProperty("scale", out var scale) || scale.GetString() != "H0")
            throw new InvalidDataException("L'escala del projecte JSON ha de ser H0.");
        if (!root.TryGetProperty("room", out var room) ||
            !room.TryGetProperty("widthMm", out var width) ||
            !room.TryGetProperty("heightMm", out var height) ||
            width.GetDecimal() <= 0 || height.GetDecimal() <= 0)
            throw new InvalidDataException("Les dimensions de la sala són absents o invàlides.");
        if (!root.TryGetProperty("levels", out var levels) || levels.ValueKind != JsonValueKind.Array ||
            levels.GetArrayLength() != 5)
            throw new InvalidDataException("El format JSON v3 requereix exactament cinc nivells.");
        if (!root.TryGetProperty("connections", out var connections) || connections.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Falta la col·lecció de connexions.");

        var levelIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var trackIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var layout = new TrackLayout
        {
            Name = RequiredString(root, "name"),
            WidthMillimetres = width.GetDecimal(),
            HeightMillimetres = height.GetDecimal(),
            GridMillimetres = 10m
        };

        foreach (var level in levels.EnumerateArray())
        {
            var externalId = RequiredId(level, "id");
            var id = StableGuid("level:" + externalId);
            if (!levelIds.TryAdd(externalId, id)) throw new InvalidDataException("Identificador de nivell duplicat.");
            layout.Levels.Add(new TrackLevel(id, RequiredString(level, "name"), RequiredDecimal(level, "elevationMm")));
        }

        foreach (var level in levels.EnumerateArray())
        {
            var externalLevelId = RequiredId(level, "id");
            var levelId = levelIds[externalLevelId];
            if (!level.TryGetProperty("tracks", out var tracks) || tracks.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("La col·lecció de vies d'un nivell no és vàlida.");
            foreach (var track in tracks.EnumerateArray())
            {
                var externalId = RequiredString(track, "id");
                var id = StableGuid("track:" + externalId);
                if (!trackIds.TryAdd(externalId, id)) throw new InvalidDataException("Identificador de via duplicat.");
                if (levelIds[RequiredId(track, "levelId")] != levelId)
                    throw new InvalidDataException("Una via referencia un nivell diferent del seu contenidor.");
                var type = RequiredString(track, "type");
                if (type is not ("straight" or "curve" or "turnout" or "block" or "special"))
                    throw new InvalidDataException("Tipus de via desconegut: " + type);
                var x = RequiredDecimal(track, "x");
                var y = RequiredDecimal(track, "y");
                if (x < 0 || x > layout.WidthMillimetres || y < 0 || y > layout.HeightMillimetres)
                    throw new InvalidDataException("Una via queda fora dels límits de la maqueta.");
                layout.Pieces.Add(new TrackPiece(id, type + ":" + RequiredString(track, "name"),
                    x, y, RequiredDecimal(track, "rotation"), levelId, "unverified"));
            }
        }

        foreach (var level in levels.EnumerateArray())
        {
            var levelId = levelIds[RequiredId(level, "id")];
            if (!level.TryGetProperty("sensors", out var sensors) || sensors.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("La col·lecció de sensors d'un nivell no és vàlida.");
            foreach (var sensor in sensors.EnumerateArray())
            {
                var id = StableGuid("sensor:" + RequiredString(sensor, "id"));
                var systemName = RequiredString(sensor, "systemName");
                var displayName = sensor.TryGetProperty("userName", out var userName) && userName.ValueKind == JsonValueKind.String
                    ? userName.GetString()! : systemName;
                layout.Sensors.Add(new FeedbackSensor(id, systemName, displayName, false));
            }
            if (!level.TryGetProperty("blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("La col·lecció de blocs d'un nivell no és vàlida.");
            foreach (var block in blocks.EnumerateArray())
            {
                var id = StableGuid("block:" + RequiredString(block, "id"));
                var pieces = block.GetProperty("trackIds").EnumerateArray()
                    .Select(x => trackIds.TryGetValue(x.GetString() ?? "", out var pieceId)
                        ? pieceId : throw new InvalidDataException("El bloc referencia una via inexistent.")).ToArray();
                layout.Blocks.Add(new LogicalBlock(id, RequiredString(block, "name"), levelId, pieces));
            }
        }

        foreach (var connection in connections.EnumerateArray())
        {
            var from = connection.GetProperty("from");
            var to = connection.GetProperty("to");
            var fromId = Resolve(trackIds, RequiredString(from, "pieceId"), "via");
            var toId = Resolve(trackIds, RequiredString(to, "pieceId"), "via");
            var levelId = Resolve(levelIds, RequiredId(connection, "levelId"), "nivell");
            if (!layout.Pieces.Any(x => x.Id == fromId && x.LevelId == levelId) ||
                !layout.Pieces.Any(x => x.Id == toId && x.LevelId == levelId))
                throw new InvalidDataException("La connexió referencia vies que no pertanyen al nivell indicat.");
            var fromPort = RequiredString(from, "port");
            var toPort = RequiredString(to, "port");
            if (fromPort is not ("A" or "B" or "C") || toPort is not ("A" or "B" or "C"))
                throw new InvalidDataException("Port de connexió no vàlid.");
            layout.Connections.Add(new TrackConnection(StableGuid("connection:" + RequiredString(connection, "id")),
                fromId, fromPort, toId, toPort,
                connection.TryGetProperty("verified", out var verified) && verified.ValueKind == JsonValueKind.True));
        }

        // This section is metadata only; importing it never enables physical control.
        if (root.TryGetProperty("commandStation", out var station) && station.ValueKind == JsonValueKind.Object)
        {
            var model = station.TryGetProperty("model", out var modelValue) && modelValue.ValueKind == JsonValueKind.String
                ? modelValue.GetString()! : "Central station";
            layout.ProtocolProfiles.Add(new RailwayProtocolProfile(StableGuid("station:" + model),
                model, "JSON v3 metadata", "127.0.0.1", 12080, false));
        }
        return layout;
    }

    private static Guid Resolve(Dictionary<string, Guid> map, string id, string kind) =>
        map.TryGetValue(id, out var value) ? value : throw new InvalidDataException("Referència a " + kind + " inexistent: " + id);

    private static Guid StableGuid(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));

    private static string RequiredString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidDataException("Falta el camp textual obligatori: " + property);
        return value.GetString()!;
    }

    private static string RequiredId(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) ||
            (value.ValueKind != JsonValueKind.String && value.ValueKind != JsonValueKind.Number))
            throw new InvalidDataException("Falta l'identificador obligatori: " + property);
        return value.ToString();
    }

    private static decimal RequiredDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || !value.TryGetDecimal(out var number))
            throw new InvalidDataException("Falta el valor numèric obligatori: " + property);
        return number;
    }
}
