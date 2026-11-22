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
    private const long MaxPackageBytes = 128 * 1024 * 1024;
    private const int MaxEntryCount = 32;

    public async Task SaveAsync(TrackLayout layout, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cal indicar el fitxer del projecte.", nameof(path));
        Validate(layout);
        cancellationToken.ThrowIfCancellationRequested();
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
                    CreatedUtc = DateTimeOffset.UtcNow.ToString("O")
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
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cal indicar el fitxer del projecte.", nameof(path));
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        if (archive.Entries.Count > MaxEntryCount || archive.Entries.Sum(entry => entry.Length) > MaxPackageBytes)
            throw new InvalidDataException("El paquet supera els límits de mida o nombre d'entrades.");
        if (archive.Entries.GroupBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new InvalidDataException("El paquet conté entrades ZIP duplicades.");
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = await ReadXmlAsync<ProjectManifest>(archive, ManifestEntry, cancellationToken);
        if (manifest.Format != "Quiral Platinum" || manifest.FormatVersion != TrackLayout.CurrentFormatVersion ||
            !DateTimeOffset.TryParse(manifest.CreatedUtc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out _))
            throw new InvalidDataException($"Manifest invàlid o format no compatible: {manifest.Format} v{manifest.FormatVersion}.");
        var document = await ReadXmlAsync<LayoutDocument>(archive, LayoutEntry, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (document.Levels is null || document.Pieces is null || document.Connections is null ||
            document.Blocks is null || document.Sensors is null || document.Routes is null || document.GeometrySegments is null || document.Signals is null || document.Locomotives is null || document.Inspections is null)
            throw new InvalidDataException("El document XML conté col·leccions nul·les.");
        var layout = document.ToDomain();
        Validate(layout);
        return layout;
    }

    private static void Validate(TrackLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (layout.Levels is null || layout.Pieces is null || layout.Connections is null || layout.Blocks is null || layout.Sensors is null || layout.Routes is null || layout.GeometrySegments is null || layout.Signals is null || layout.Locomotives is null || layout.Inspections is null)
            throw new InvalidDataException("El projecte conté col·leccions nul·les.");
        if (layout.Id == Guid.Empty || string.IsNullOrWhiteSpace(layout.Name) ||
            layout.WidthMillimetres <= 0 || layout.HeightMillimetres <= 0 || layout.GridMillimetres <= 0)
            throw new InvalidDataException("El projecte conté identificadors, nom o dimensions invàlids.");

        static void EnsureUniqueIds<T>(IEnumerable<T> items, Func<T, Guid> idSelector, string collection)
        {
            var ids = items.Select(idSelector).ToArray();
            if (ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
                throw new InvalidDataException($"La col·lecció {collection} conté identificadors buits o duplicats.");
        }

        EnsureUniqueIds(layout.Levels, x => x.Id, "nivells");
        if (layout.Levels.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            throw new InvalidDataException("Hi ha nivells sense nom.");
        EnsureUniqueIds(layout.Pieces, x => x.Id, "peces");
        EnsureUniqueIds(layout.Connections, x => x.Id, "connexions");
        EnsureUniqueIds(layout.Blocks, x => x.Id, "blocs");
        EnsureUniqueIds(layout.Sensors, x => x.Id, "sensors");
        EnsureUniqueIds(layout.Routes, x => x.Id, "itineraris");
        EnsureUniqueIds(layout.GeometrySegments, x => x.Id, "geometria de vies");
        EnsureUniqueIds(layout.Signals, x => x.Id, "senyals");
        EnsureUniqueIds(layout.Locomotives, x => x.Id, "locomotores");
        EnsureUniqueIds(layout.Inspections, x => x.Id, "inspeccions");

        var levelIds = layout.Levels.Select(x => x.Id).ToHashSet();
        var pieceIds = layout.Pieces.Select(x => x.Id).ToHashSet();
        var blockIds = layout.Blocks.Select(x => x.Id).ToHashSet();
        if (layout.GeometrySegments.Any(x => !levelIds.Contains(x.LevelId) || !Enum.IsDefined(x.Kind)))
            throw new InvalidDataException("La geometria de vies conté nivells inexistents o tipus desconeguts.");

        if (layout.Pieces.Any(x => string.IsNullOrWhiteSpace(x.CatalogueCode) ||
            !levelIds.Contains(x.LevelId) ||
            x.GeometryStatus is not ("confirmed" or "partial" or "unverified")))
            throw new InvalidDataException("Hi ha peces sense codi de catàleg o assignades a nivells inexistents.");

        if (layout.Connections.Any(x => x.FromPieceId == x.ToPieceId ||
            !pieceIds.Contains(x.FromPieceId) || !pieceIds.Contains(x.ToPieceId) ||
            string.IsNullOrWhiteSpace(x.FromPort) || string.IsNullOrWhiteSpace(x.ToPort)))
            throw new InvalidDataException("El projecte conté connexions invàlides o referències a peces inexistents.");

        if (layout.Blocks.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.PieceIds is null ||
            !levelIds.Contains(x.LevelId) || x.PieceIds.Any(id => !pieceIds.Contains(id)) ||
            x.PieceIds.Distinct().Count() != x.PieceIds.Count))
            throw new InvalidDataException("Hi ha blocs amb nivell, peces o noms invàlids.");

        if (layout.Sensors.Any(x => string.IsNullOrWhiteSpace(x.SystemName) || string.IsNullOrWhiteSpace(x.DisplayName)) ||
            layout.Sensors.GroupBy(x => x.SystemName, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new InvalidDataException("Hi ha sensors sense identificador de sistema o nom visible.");

        if (layout.Routes.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.BlockIds is null || x.BlockIds.Any(id => !blockIds.Contains(id)) ||
            x.BlockIds.Distinct().Count() != x.BlockIds.Count))
            throw new InvalidDataException("Hi ha itineraris amb blocs inexistents o referències duplicades.");
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
        var settings = new System.Xml.XmlReaderSettings
        {
            DtdProcessing = System.Xml.DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxEntryBytes,
            MaxCharactersFromEntities = 0,
            IgnoreComments = true
        };
        using var reader = System.Xml.XmlReader.Create(input, settings);
        var serializer = new XmlSerializer(typeof(T));
        try
        {
            return (T)(serializer.Deserialize(reader) ?? throw new InvalidDataException($"No s'ha pogut llegir {name}."));
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidDataException($"L'entrada XML {name} no té un format vàlid.", ex);
        }
    }

    [XmlRoot("PlatinumManifest")]
    public sealed class ProjectManifest
    {
        public string Format { get; set; } = "Quiral Platinum";
        public int FormatVersion { get; set; } = 1;
        public string CreatedUtc { get; set; } = "";
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
        [XmlArray] public List<GeometrySegmentDocument> GeometrySegments { get; set; } = [];
        [XmlArray] public List<ConnectionDocument> Connections { get; set; } = [];
        [XmlArray] public List<BlockDocument> Blocks { get; set; } = [];
        [XmlArray] public List<SensorDocument> Sensors { get; set; } = [];
        [XmlArray] public List<RouteDocument> Routes { get; set; } = [];
        [XmlArray] public List<SignalDocument> Signals { get; set; } = [];
        [XmlArray] public List<LocomotiveDocument> Locomotives { get; set; } = [];
        [XmlArray] public List<InspectionDocument> Inspections { get; set; } = [];

        public static LayoutDocument From(TrackLayout x) => new()
        {
            Id=x.Id, Name=x.Name, WidthMillimetres=x.WidthMillimetres, HeightMillimetres=x.HeightMillimetres, GridMillimetres=x.GridMillimetres,
            Levels=x.Levels.Select(v=>new LevelDocument{Id=v.Id,Name=v.Name,ElevationMillimetres=v.ElevationMillimetres}).ToList(),
            Pieces=x.Pieces.Select(v=>new PieceDocument{Id=v.Id,CatalogueCode=v.CatalogueCode,X=v.X,Y=v.Y,RotationDegrees=v.RotationDegrees,LevelId=v.LevelId,GeometryStatus=v.GeometryStatus}).ToList(),
            GeometrySegments=x.GeometrySegments.Select(v=>new GeometrySegmentDocument{Id=v.Id,Kind=v.Kind,StartX=v.StartX,StartY=v.StartY,EndX=v.EndX,EndY=v.EndY,ControlX=v.ControlX,ControlY=v.ControlY,LevelId=v.LevelId}).ToList(),
            Connections=x.Connections.Select(v=>new ConnectionDocument{Id=v.Id,FromPieceId=v.FromPieceId,FromPort=v.FromPort,ToPieceId=v.ToPieceId,ToPort=v.ToPort,PhysicallyVerified=v.PhysicallyVerified}).ToList(),
            Blocks=x.Blocks.Select(v=>new BlockDocument{Id=v.Id,Name=v.Name,LevelId=v.LevelId,PieceIds=v.PieceIds.ToList()}).ToList(),
            Sensors=x.Sensors.Select(v=>new SensorDocument{Id=v.Id,SystemName=v.SystemName,DisplayName=v.DisplayName,PhysicallyVerified=v.PhysicallyVerified}).ToList(),
            Routes=x.Routes.Select(v=>new RouteDocument{Id=v.Id,Name=v.Name,BlockIds=v.BlockIds.ToList()}).ToList(),
            Signals=x.Signals.Select(v=>new SignalDocument{Id=v.Id,Name=v.Name,Aspect=v.Aspect,ProtectedBlockId=v.ProtectedBlockId,PhysicallyVerified=v.PhysicallyVerified}).ToList(),
            Locomotives=x.Locomotives.Select(v=>new LocomotiveDocument{Id=v.Id,Name=v.Name,Address=v.Address,Protocol=v.Protocol,SimulationOnly=v.SimulationOnly}).ToList(),
            Inspections=x.Inspections.Select(v=>new InspectionDocument{Id=v.Id,Item=v.Item,OperatorDeclaredComplete=v.OperatorDeclaredComplete,Notes=v.Notes,CheckedUtc=v.CheckedUtc.ToString("O")}).ToList()
        };
        public TrackLayout ToDomain() => new()
        {
            Id=Id,Name=Name,WidthMillimetres=WidthMillimetres,HeightMillimetres=HeightMillimetres,GridMillimetres=GridMillimetres,
            Levels=Levels.Select(v=>new TrackLevel(v.Id,v.Name,v.ElevationMillimetres)).ToList(),
            Pieces=Pieces.Select(v=>new TrackPiece(v.Id,v.CatalogueCode,v.X,v.Y,v.RotationDegrees,v.LevelId,string.IsNullOrWhiteSpace(v.GeometryStatus) ? "unverified" : v.GeometryStatus)).ToList(),
            GeometrySegments=GeometrySegments.Select(v=>new TrackGeometrySegment(v.Id,v.Kind,v.StartX,v.StartY,v.EndX,v.EndY,v.ControlX,v.ControlY,v.LevelId)).ToList(),
            Connections=Connections.Select(v=>new TrackConnection(v.Id,v.FromPieceId,v.FromPort,v.ToPieceId,v.ToPort,v.PhysicallyVerified)).ToList(),
            Blocks=Blocks.Select(v=>new LogicalBlock(v.Id,v.Name,v.LevelId,v.PieceIds)).ToList(),
            Sensors=Sensors.Select(v=>new FeedbackSensor(v.Id,v.SystemName,v.DisplayName,v.PhysicallyVerified)).ToList(),
            Routes=Routes.Select(v=>new RouteDefinition(v.Id,v.Name,v.BlockIds)).ToList(),
            Signals=Signals.Select(v=>new SignalDefinition(v.Id,v.Name,v.Aspect,v.ProtectedBlockId,v.PhysicallyVerified)).ToList(),
            Locomotives=Locomotives.Select(v=>new LocomotiveProfile(v.Id,v.Name,v.Address,v.Protocol,v.SimulationOnly)).ToList(),
            Inspections=Inspections.Select(v=>new PhysicalInspectionRecord(v.Id,v.Item,v.OperatorDeclaredComplete,v.Notes,DateTimeOffset.Parse(v.CheckedUtc,System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind))).ToList()
        };
    }
    public sealed class LevelDocument { public Guid Id {get;set;} public string Name {get;set;}=""; public decimal ElevationMillimetres {get;set;} }
    public sealed class PieceDocument { public Guid Id {get;set;} public string CatalogueCode {get;set;}=""; public decimal X {get;set;} public decimal Y {get;set;} public decimal RotationDegrees {get;set;} public Guid LevelId {get;set;} public string GeometryStatus {get;set;}="unverified"; }
    public sealed class GeometrySegmentDocument { public Guid Id {get;set;} public TrackGeometryKind Kind {get;set;} public decimal StartX {get;set;} public decimal StartY {get;set;} public decimal EndX {get;set;} public decimal EndY {get;set;} public decimal ControlX {get;set;} public decimal ControlY {get;set;} public Guid LevelId {get;set;} }
    public sealed class ConnectionDocument { public Guid Id {get;set;} public Guid FromPieceId {get;set;} public string FromPort {get;set;}=""; public Guid ToPieceId {get;set;} public string ToPort {get;set;}=""; public bool PhysicallyVerified {get;set;} }
    public sealed class BlockDocument { public Guid Id {get;set;} public string Name {get;set;}=""; public Guid LevelId {get;set;} [XmlArrayItem("PieceId")] public List<Guid> PieceIds {get;set;}=[]; }
    public sealed class SensorDocument { public Guid Id {get;set;} public string SystemName {get;set;}=""; public string DisplayName {get;set;}=""; public bool PhysicallyVerified {get;set;} }
    public sealed class RouteDocument { public Guid Id {get;set;} public string Name {get;set;}=""; [XmlArrayItem("BlockId")] public List<Guid> BlockIds {get;set;}=[]; }
    public sealed class SignalDocument { public Guid Id {get;set;} public string Name {get;set;}=""; public string Aspect {get;set;}="unknown"; public Guid? ProtectedBlockId {get;set;} public bool PhysicallyVerified {get;set;} }
    public sealed class LocomotiveDocument { public Guid Id {get;set;} public string Name {get;set;}=""; public int Address {get;set;} public string Protocol {get;set;}="simulation"; public bool SimulationOnly {get;set;}=true; }
    public sealed class InspectionDocument { public Guid Id {get;set;} public string Item {get;set;}=""; public bool OperatorDeclaredComplete {get;set;} public string Notes {get;set;}=""; public string CheckedUtc {get;set;}=""; }
}
