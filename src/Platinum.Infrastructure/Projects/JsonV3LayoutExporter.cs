using System.Text.Json;
using Platinum.Domain.Layout;
namespace Platinum.Infrastructure.Projects;
public sealed class JsonV3LayoutExporter
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public string Export(TrackLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (layout.Levels.Count != 5) throw new InvalidDataException("El format JSON v3 exige exactament cinc nivells.");
        if (layout.WidthMillimetres != 3000m || layout.HeightMillimetres != 1800m) throw new InvalidDataException("El format JSON v3 exigeix una sala de 3000 × 1800 mm.");
        var levelIds=layout.Levels.Select(x=>x.Id).ToHashSet();
        var pieceIds=layout.Pieces.Select(x=>x.Id).ToHashSet();
        if(layout.Pieces.Any(x=>!levelIds.Contains(x.LevelId)||x.X<0||x.X>3000||x.Y<0||x.Y>1800)) throw new InvalidDataException("Hi ha vies fora dels nivells o dels límits de la sala.");
        if(layout.Blocks.Any(x=>!levelIds.Contains(x.LevelId)||x.PieceIds.Any(id=>!pieceIds.Contains(id)))) throw new InvalidDataException("Hi ha blocs amb referències invàlides.");
        if(layout.Connections.Any(x=>!pieceIds.Contains(x.FromPieceId)||!pieceIds.Contains(x.ToPieceId)||x.FromPieceId==x.ToPieceId||layout.Pieces.Single(p=>p.Id==x.FromPieceId).LevelId!=layout.Pieces.Single(p=>p.Id==x.ToPieceId).LevelId)) throw new InvalidDataException("Hi ha connexions invàlides o entre nivells.");
        if(layout.ProtocolProfiles.Any(x=>x.PhysicalCommandsEnabled)) throw new InvalidDataException("No s'exporten perfils amb ordres físiques activades.");
        var blockByPiece=layout.Blocks.SelectMany(b=>b.PieceIds.Select(id=>(id,b.Id))).GroupBy(x=>x.id).ToDictionary(g=>g.Key,g=>g.First().Id);
        var levels=layout.Levels.Select((level,index)=>new {
            id=level.Id.ToString("D"), name=level.Name, elevationMm=level.ElevationMillimetres,
            tracks=layout.Pieces.Where(p=>p.LevelId==level.Id).Select(p=>{
                var parts=p.CatalogueCode.Split(':',2);
                var type=parts.Length==2 && (parts[0]=="straight"||parts[0]=="curve"||parts[0]=="turnout"||parts[0]=="block"||parts[0]=="special")?parts[0]:"special";
                return new { id=p.Id.ToString("D"), type, name=parts.Length==2?parts[1]:p.CatalogueCode, x=p.X,y=p.Y,rotation=p.RotationDegrees,levelId=level.Id.ToString("D"),blockId=blockByPiece.TryGetValue(p.Id,out var bid)?bid.ToString("D"):"",sensorName="",geometry=new {lengthMm=0,radiusMm=0,angleDeg=0} };
            }).ToArray(),
            blocks=layout.Blocks.Where(b=>b.LevelId==level.Id).Select(b=>new {id=b.Id.ToString("D"),name=b.Name,trackIds=b.PieceIds.Select(x=>x.ToString("D")).ToArray(),lengthMm=0,sensorIds=Array.Empty<string>(),verified=false,importedPlaceholder=false}).ToArray(),
            sensors=index==0?layout.Sensors.Select(s=>new {id=s.Id.ToString("D"),systemName=s.SystemName,userName=s.DisplayName,invert=false,verified=false,importedPlaceholder=false}).ToArray():Array.Empty<object>()
        }).ToArray();
        var connections=layout.Connections.Select(c=>new {id=c.Id.ToString("D"),levelId=layout.Pieces.Single(p=>p.Id==c.FromPieceId).LevelId.ToString("D"),from=new {pieceId=c.FromPieceId.ToString("D"),port=c.FromPort},to=new {pieceId=c.ToPieceId.ToString("D"),port=c.ToPort},verified=false,method="native-export-unverified"}).ToArray();
        var station=layout.ProtocolProfiles.FirstOrDefault();
        return JsonSerializer.Serialize(new {schemaVersion=3,name=layout.Name,scale="H0",trackSystem="Märklin C",room=new {widthMm=3000,heightMm=1800},levelSpacingMm=110,
            commandStation=station is null?null:new {manufacturer="Unknown",model=station.Name,protocol=station.Kind,physicalCommandsEnabled=false},
            feedback=new {bus="S88",verified=false},levels,connections},Options);
    }
}