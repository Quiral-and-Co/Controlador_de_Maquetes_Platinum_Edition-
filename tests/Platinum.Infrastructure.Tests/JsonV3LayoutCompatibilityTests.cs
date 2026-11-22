using System.IO;
using System.Text.Json;
using Platinum.Infrastructure.Projects;
using Xunit;
namespace Platinum.Infrastructure.Tests;
public sealed class JsonV3LayoutCompatibilityTests
{
    private const string Fixture = """
    {"schemaVersion":3,"name":"Compatibilitat H0","scale":"H0","trackSystem":"Märklin C","room":{"widthMm":3000,"heightMm":1800},"levelSpacingMm":110,
    "commandStation":{"manufacturer":"Märklin","model":"Central Station 2","article":"60215","protocol":"Märklin CAN over TCP/IP"},
    "feedback":{"bus":"S88","modules":["60881","60882"]},
    "levels":[
    {"id":1,"name":"N1","elevationMm":0,"tracks":[{"id":"t1","type":"straight","name":"C1","x":100,"y":100,"rotation":0,"levelId":1,"blockId":"b1","sensorName":""},{"id":"t2","type":"curve","name":"C2","x":200,"y":100,"rotation":90,"levelId":1,"blockId":"b1","sensorName":""}],"blocks":[{"id":"b1","name":"Bloc 1","trackIds":["t1","t2"],"lengthMm":100,"sensorIds":["s1"],"verified":false}],"sensors":[{"id":"s1","systemName":"LS101","userName":"Sensor 1","invert":false,"verified":false}]},
    {"id":2,"name":"N2","elevationMm":110,"tracks":[],"blocks":[],"sensors":[]},{"id":3,"name":"N3","elevationMm":220,"tracks":[],"blocks":[],"sensors":[]},{"id":4,"name":"N4","elevationMm":330,"tracks":[],"blocks":[],"sensors":[]},{"id":5,"name":"N5","elevationMm":440,"tracks":[],"blocks":[],"sensors":[]}],
    "connections":[{"id":"c1","levelId":1,"from":{"pieceId":"t1","port":"B"},"to":{"pieceId":"t2","port":"A"},"verified":false,"method":"manual"}]}
    """;
    [Fact] public void ImportsExportsAndReimportsV3PreservingSupportedIdsAndSafety()
    {
        var importer=new JsonV3LayoutImporter(); var original=importer.Import(Fixture);
        var json=new JsonV3LayoutExporter().Export(original); using var parsed=JsonDocument.Parse(json);
        Assert.Equal(3,parsed.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(5,parsed.RootElement.GetProperty("levels").GetArrayLength());
        var roundTrip=importer.Import(json);
        Assert.Equal(original.Levels.Select(x=>x.Id),roundTrip.Levels.Select(x=>x.Id));
        Assert.Equal(original.Pieces.Select(x=>x.Id),roundTrip.Pieces.Select(x=>x.Id));
        Assert.Equal(original.Blocks.Select(x=>x.Id),roundTrip.Blocks.Select(x=>x.Id));
        Assert.Equal(original.Sensors.Select(x=>x.Id),roundTrip.Sensors.Select(x=>x.Id));
        Assert.Equal(original.Connections.Select(x=>x.Id),roundTrip.Connections.Select(x=>x.Id));
        Assert.All(roundTrip.ProtocolProfiles,p=>Assert.False(p.PhysicalCommandsEnabled));
        Assert.All(roundTrip.Pieces,p=>Assert.Equal("unverified",p.GeometryStatus));
        Assert.All(roundTrip.Connections,c=>Assert.False(c.PhysicallyVerified));
    }
    [Fact] public void ExportRejectsUnsupportedRoomSize(){var x=new JsonV3LayoutImporter().Import(Fixture);x.WidthMillimetres=2500;Assert.Throws<InvalidDataException>(()=>new JsonV3LayoutExporter().Export(x));}
    [Fact] public void ImportRejectsWrongVersion()=>Assert.Throws<InvalidDataException>(()=>new JsonV3LayoutImporter().Import("{\"schemaVersion\":2}"));
}