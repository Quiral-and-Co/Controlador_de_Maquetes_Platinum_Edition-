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
            var layout = new TrackLayout
            {
                Name = "Circuit de prova",
                Levels = [new TrackLevel(levelId, "Principal", 0m)],
                Pieces = [new TrackPiece(pieceId, "Märklin-24188", 120m, 240m, 90m, levelId)],
                Blocks = [new LogicalBlock(Guid.NewGuid(), "Bloc A", levelId, [pieceId])],
                Sensors = [new FeedbackSensor(Guid.NewGuid(), "CS2:S88:1", "Sensor 1")],
                Routes = [new RouteDefinition(Guid.NewGuid(), "Ruta A", [])]
            };

            var store = new PlatinumPackageStore();
            await store.SaveAsync(layout, path);
            var loaded = await store.LoadAsync(path);

            Assert.Equal(layout.Id, loaded.Id);
            Assert.Equal(layout.Name, loaded.Name);
            Assert.Equal(layout.Pieces, loaded.Pieces);
            Assert.Equal(layout.Blocks[0].PieceIds, loaded.Blocks[0].PieceIds);
            Assert.Equal(layout.Sensors[0].SystemName, loaded.Sensors[0].SystemName);

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

}
