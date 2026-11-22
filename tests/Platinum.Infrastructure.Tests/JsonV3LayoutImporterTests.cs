using System.IO;
using Platinum.Infrastructure.Projects;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class JsonV3LayoutImporterTests
{
    [Fact]
    public void ImportsFiveLevelV3LayoutWithoutEnablingPhysicalOutputs()
    {
        const string json = """
        {
          "schemaVersion": 3, "name": "Test H0", "scale": "H0",
          "trackSystem": "Märklin C",
          "room": { "widthMm": 3000, "heightMm": 1800 },
          "levelSpacingMm": 110,
          "levels": [
            { "id": 1, "name": "N1", "elevationMm": 0, "tracks": [
              { "id": "t1", "type": "straight", "name": "C1", "x": 100, "y": 100, "rotation": 0, "levelId": 1, "blockId": "", "sensorName": "" }
            ], "blocks": [
              { "id": "b1", "name": "Bloc 1", "trackIds": ["t1"], "lengthMm": 100, "sensorIds": [], "verified": false }
            ], "sensors": [] },
            { "id": 2, "name": "N2", "elevationMm": 110, "tracks": [], "blocks": [], "sensors": [] },
            { "id": 3, "name": "N3", "elevationMm": 220, "tracks": [], "blocks": [], "sensors": [] },
            { "id": 4, "name": "N4", "elevationMm": 330, "tracks": [], "blocks": [], "sensors": [] },
            { "id": 5, "name": "N5", "elevationMm": 440, "tracks": [], "blocks": [], "sensors": [] }
          ],
          "connections": [],
          "commandStation": { "model": "Central Station 2" }
        }
        """;

        var layout = new JsonV3LayoutImporter().Import(json);

        Assert.Equal("Test H0", layout.Name);
        Assert.Equal(5, layout.Levels.Count);
        Assert.Single(layout.Pieces);
        Assert.Single(layout.Blocks);
        Assert.Single(layout.ProtocolProfiles);
        Assert.All(layout.ProtocolProfiles, profile => Assert.False(profile.PhysicalCommandsEnabled));
        Assert.All(layout.Pieces, piece => Assert.Equal("unverified", piece.GeometryStatus));
    }

    [Fact]
    public void RejectsNonV3Documents()
    {
        Assert.Throws<InvalidDataException>(() => new JsonV3LayoutImporter().Import("{\"schemaVersion\":2}"));
    }
}
