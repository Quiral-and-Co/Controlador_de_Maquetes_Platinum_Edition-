using System.IO.Compression;
using System.Xml.Serialization;
using Platinum.Application.Projects;
using Platinum.Domain.Layout;

namespace Platinum.Infrastructure.Projects;

/// <summary>
/// Native .platinum project package: ZIP container with manifest.xml and layout.xml.
/// JSON v3 is deliberately not used as the persistence contract.
/// </summary>
public sealed class PlatinumPackageStore : IProjectStore
{
    private const string ManifestEntry = "manifest.xml";
    private const string LayoutEntry = "layout.xml";
    private const long MaxEntryBytes = 64 * 1024 * 1024;

    public async Task SaveAsync(TrackLayout layout, string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, true))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
            {
                await WriteXmlAsync(archive, ManifestEntry, new ProjectManifest
                {
                    Format = "Quiral Platinum",
                    FormatVersion = TrackLayout.CurrentFormatVersion,
                    CreatedUtc = DateTimeOffset.UtcNow
                }, cancellationToken);
                await WriteXmlAsync(archive, LayoutEntry, LayoutDocument.From(layout), cancellationToken);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public async Task<TrackLayout> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var manifest = await ReadXmlAsync<ProjectManifest>(archive, ManifestEntry, cancellationToken);
        if (manifest.Format != "Quiral Platinum" || manifest.FormatVersion != TrackLayout.CurrentFormatVersion)
            throw new InvalidDataException($"Format de projecte no compatible: {manifest.Format} v{manifest.FormatVersion}.");
        var document = await ReadXmlAsync<LayoutDocument>(archive, LayoutEntry, cancellationToken);
        var layout = document.ToDomain();
        Validate(layout);
        return layout;
    }

    private static void Validate(TrackLayout layout)
    {
        if (layout.Id == Guid.Empty || layout.WidthMillimetres <= 0 || layout.HeightMillimetres <= 0 || layout.GridMillimetres <= 0)
            throw new InvalidDataException("El projecte conté identificadors o dimensions invàlids.");
        var pieceIds = layout.Pieces.Select(x => x.Id).ToHashSet();
        if (pieceIds.Count != layout.Pieces.Count || layout.Pieces.Any(x => x.Id == Guid.Empty || !layout.Levels.Any(l => l.Id == x.LevelId)))
            throw new InvalidDataException("El projecte conté peces duplicades o nivells inexistents.");
        if (layout.Connections.Any(x => !pieceIds.Contains(x.FromPieceId) || !pieceIds.Contains(x.ToPieceId)))
            throw new InvalidDataException("El projecte conté connexions a peces inexistents.");
    }

    private static async Task WriteXmlAsync<T>(ZipArchive archive, string name, T value, CancellationToken ct)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        await using var output = entry.Open();
        var serializer = new XmlSerializer(typeof(T));
        using var writer = System.Xml.XmlWriter.Create(output, new System.Xml.XmlWriterSettings { Async = true, Indent = true, Encoding = new System.Text.UTF8Encoding(false) });
        serializer.Serialize(writer, value);
        await writer.FlushAsync();
        ct.ThrowIfCancellationRequested();
    }

    private static async Task<T> ReadXmlAsync<T>(ZipArchive archive, string name, CancellationToken ct)
    {
        var entry = archive.GetEntry(name) ?? throw new InvalidDataException($"Falta l'entrada {name}.");
        if (entry.Length > MaxEntryBytes) throw new InvalidDataException($"L'entrada {name} supera la mida permesa.");
        await using var input = entry.Open();
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory, ct);
        memory.Position = 0;
        var serializer = new XmlSerializer(typeof(T));
        return (T)(serializer.Deserialize(memory) ?? throw new InvalidDataException($"No s'ha pogut llegir {name}."));
    }

    [XmlRoot("PlatinumManifest")]
    public sealed class ProjectManifest
    {
        public string Format { get; set; } = "Quiral Platinum";
        public int FormatVersion { get; set; } = 1;
        public DateTimeOffset CreatedUtc { get; set; }
    }

    [XmlRoot("Layout")]
    public sealed class LayoutDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public decimal WidthMillimetres { get; set; }
        public decimal HeightMillimetres { get; set; }
        public decimal GridMillimetres { get; set; }
        [XmlArray] public List<LevelDocument> Levels { get; set; } = [];
        [XmlArray] public List<PieceDocument> Pieces { get; set; } = [];
        [XmlArray] public List<ConnectionDocument> Connections { get; set; } = [];
        [XmlArray] public List<BlockDocument> Blocks { get; set; } = [];
        [XmlArray] public List<SensorDocument> Sensors { get; set; } = [];
        [XmlArray] public List<RouteDocument> Routes { get; set; } = [];

        public static LayoutDocument From(TrackLayout x) => new()
        {
            Id=x.Id, Name=x.Name, WidthMillimetres=x.WidthMillimetres, HeightMillimetres=x.HeightMillimetres, GridMillimetres=x.GridMillimetres,
            Levels=x.Levels.Select(v=>new LevelDocument{Id=v.Id,Name=v.Name,ElevationMillimetres=v.ElevationMillimetres}).ToList(),
            Pieces=x.Pieces.Select(v=>new PieceDocument{Id=v.Id,CatalogueCode=v.CatalogueCode,X=v.X,Y=v.Y,RotationDegrees=v.RotationDegrees,LevelId=v.LevelId}).ToList(),
            Connections=x.Connections.Select(v=>new ConnectionDocument{Id=v.Id,FromPieceId=v.FromPieceId,FromPort=v.FromPort,ToPieceId=v.ToPieceId,ToPort=v.ToPort,PhysicallyVerified=v.PhysicallyVerified}).ToList(),
            Blocks=x.Blocks.Select(v=>new BlockDocument{Id=v.Id,Name=v.Name,LevelId=v.LevelId,PieceIds=v.PieceIds.ToList()}).ToList(),
            Sensors=x.Sensors.Select(v=>new SensorDocument{Id=v.Id,SystemName=v.SystemName,DisplayName=v.DisplayName,PhysicallyVerified=v.PhysicallyVerified}).ToList(),
            Routes=x.Routes.Select(v=>new RouteDocument{Id=v.Id,Name=v.Name,BlockIds=v.BlockIds.ToList()}).ToList()
        };
        public TrackLayout ToDomain() => new()
        {
            Id=Id,Name=Name,WidthMillimetres=WidthMillimetres,HeightMillimetres=HeightMillimetres,GridMillimetres=GridMillimetres,
            Levels=Levels.Select(v=>new TrackLevel(v.Id,v.Name,v.ElevationMillimetres)).ToList(),
            Pieces=Pieces.Select(v=>new TrackPiece(v.Id,v.CatalogueCode,v.X,v.Y,v.RotationDegrees,v.LevelId)).ToList(),
            Connections=Connections.Select(v=>new TrackConnection(v.Id,v.FromPieceId,v.FromPort,v.ToPieceId,v.ToPort,v.PhysicallyVerified)).ToList(),
            Blocks=Blocks.Select(v=>new LogicalBlock(v.Id,v.Name,v.LevelId,v.PieceIds)).ToList(),
            Sensors=Sensors.Select(v=>new FeedbackSensor(v.Id,v.SystemName,v.DisplayName,v.PhysicallyVerified)).ToList(),
            Routes=Routes.Select(v=>new RouteDefinition(v.Id,v.Name,v.BlockIds)).ToList()
        };
    }
    public sealed class LevelDocument { public Guid Id {get;set;} public string Name {get;set;}=""; public decimal ElevationMillimetres {get;set;} }
    public sealed class PieceDocument { public Guid Id {get;set;} public string CatalogueCode {get;set;}=""; public decimal X {get;set;} public decimal Y {get;set;} public decimal RotationDegrees {get;set;} public Guid LevelId {get;set;} }
    public sealed class ConnectionDocument { public Guid Id {get;set;} public Guid FromPieceId {get;set;} public string FromPort {get;set;}=""; public Guid ToPieceId {get;set;} public string ToPort {get;set;}=""; public bool PhysicallyVerified {get;set;} }
    public sealed class BlockDocument { public Guid Id {get;set;} public string Name {get;set;}=""; public Guid LevelId {get;set;} [XmlArrayItem("PieceId")] public List<Guid> PieceIds {get;set;}=[]; }
    public sealed class SensorDocument { public Guid Id {get;set;} public string SystemName {get;set;}=""; public string DisplayName {get;set;}=""; public bool PhysicallyVerified {get;set;} }
    public sealed class RouteDocument { public Guid Id {get;set;} public string Name {get;set;}=""; [XmlArrayItem("BlockId")] public List<Guid> BlockIds {get;set;}=[]; }
}
