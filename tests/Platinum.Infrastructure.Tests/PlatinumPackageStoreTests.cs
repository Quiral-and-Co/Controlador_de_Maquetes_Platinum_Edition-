using System.IO.Compression;
using Platinum.Domain.Layout;
using Platinum.Infrastructure.Projects;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class PlatinumPackageStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsNativeProjectPackage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"platinum-{Guid.NewGuid():N}.platinum");
        try
        {
            var levelId = Guid.NewGuid();
            var pieceId = Guid.NewGuid();
            var secondPieceId = Guid.NewGuid();
            var connection = new TrackConnection(Guid.NewGuid(), pieceId, "logical", secondPieceId, "logical", false);
            var layout = new TrackLayout
            {
                Name = "Circuit de prova",
                Levels = [new TrackLevel(levelId, "Principal", 0m)],
                Pieces = [new TrackPiece(pieceId, "24188", 120m, 240m, 90m, levelId, "partial"), new TrackPiece(secondPieceId, "24172", 300m, 240m, 0m, levelId, "confirmed")],
                Connections = [connection],
                GeometrySegments = [new TrackGeometrySegment(Guid.NewGuid(), TrackGeometryKind.Curve, 100m, 100m, 400m, 250m, 230m, 20m, levelId)],
                Blocks = [new LogicalBlock(Guid.NewGuid(), "Bloc A", levelId, [pieceId], true)],
                Sensors = [new FeedbackSensor(Guid.NewGuid(), "CS2:S88:1", "Sensor 1")],
                Routes = [new RouteDefinition(Guid.NewGuid(), "Ruta A", [], true)]
            };

            var store = new PlatinumPackageStore();
            await store.SaveAsync(layout, path);
            var loaded = await store.LoadAsync(path);

            Assert.Equal(layout.Id, loaded.Id);
            Assert.Equal(layout.Name, loaded.Name);
            Assert.Equal(layout.Pieces, loaded.Pieces);
            Assert.Equal(layout.Connections, loaded.Connections);
            Assert.False(loaded.Connections[0].PhysicallyVerified);
            Assert.Equal("partial", loaded.Pieces[0].GeometryStatus);
            Assert.Equal(layout.GeometrySegments, loaded.GeometrySegments);
            Assert.Equal(layout.Blocks[0].PieceIds, loaded.Blocks[0].PieceIds);
            Assert.Equal(layout.Sensors[0].SystemName, loaded.Sensors[0].SystemName);
            Assert.True(loaded.Blocks[0].Occupied);
            Assert.True(loaded.Routes[0].Reserved);

            using var archive = ZipFile.OpenRead(path);
            Assert.NotNull(archive.GetEntry("manifest.xml"));
            Assert.NotNull(archive.GetEntry("layout.xml"));
            Assert.Null(archive.GetEntry("layout.json"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Load_RejectsUnsupportedFormatVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"platinum-invalid-{Guid.NewGuid():N}.platinum");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("manifest.xml");
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync("<PlatinumManifest><Format>Quiral Platinum</Format><FormatVersion>999</FormatVersion><CreatedUtc>2026-01-01T00:00:00Z</CreatedUtc></PlatinumManifest>");
            }
            await Assert.ThrowsAsync<InvalidDataException>(() => new PlatinumPackageStore().LoadAsync(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
    [Fact]
    public void PhysicalControlGate_DeniesCommandsByDefault()
    {
        var gate = new Platinum.Application.Safety.PhysicalControlGate();
        var state = new Platinum.Domain.Safety.HardwareSafetyState();

        Assert.False(gate.CanIssueCommands(state));
        Assert.Throws<InvalidOperationException>(() => gate.EnsureCommandsAllowed(state));
    }

    [Fact]
    public void PhysicalControlGate_RequiresEveryRuntimeSafetyCondition()
    {
        var gate = new Platinum.Application.Safety.PhysicalControlGate();
        var state = new Platinum.Domain.Safety.HardwareSafetyState(
            JmriConnected: true,
            CommandStationConnected: true,
            FeedbackHealthy: true,
            EmergencyStopCircuitVerified: true,
            OperatorSafetyAcknowledged: true,
            PhysicalControlArmed: false);

        Assert.False(gate.CanIssueCommands(state));
    }

    [Fact]
    public async Task Save_RejectsConnectionsToMissingPieces()
    {
        var path = Path.Combine(Path.GetTempPath(), $"platinum-invalid-layout-{Guid.NewGuid():N}.platinum");
        var layout = new TrackLayout
        {
            Levels = [new TrackLevel(Guid.NewGuid(), "Principal", 0m)],
            Connections = [new TrackConnection(Guid.NewGuid(), Guid.NewGuid(), "A", Guid.NewGuid(), "B")]
        };

        await Assert.ThrowsAsync<InvalidDataException>(() => new PlatinumPackageStore().SaveAsync(layout, path));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Load_RejectsXmlDocumentTypeDeclarations()
    {
        var path = Path.Combine(Path.GetTempPath(), $"platinum-dtd-{Guid.NewGuid():N}.platinum");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("manifest.xml");
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync("<!DOCTYPE PlatinumManifest [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><PlatinumManifest><Format>&xxe;</Format><FormatVersion>1</FormatVersion></PlatinumManifest>");
            }

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new PlatinumPackageStore().LoadAsync(path));
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.IsType<System.Xml.XmlException>(exception.InnerException.InnerException);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

}
