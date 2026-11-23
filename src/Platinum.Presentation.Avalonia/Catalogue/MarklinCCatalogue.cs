using System.Text.Json;

namespace Platinum.Presentation.Avalonia.Catalogue;

public sealed record CataloguePort(string Id,decimal X,decimal Y,string Accuracy);
public sealed record CatalogueTrackPath(string FromPort,string ToPort,string Kind,decimal ControlX=0m,decimal ControlY=0m);
public sealed record CatalogueItem(string Ref,string Name,string Category,string Type,decimal? LengthMm,decimal? RadiusMm,decimal? AngleDeg,string GeometryStatus,string? Notes)
{
    public string DisplayName=>Ref+" · "+Name;
    public string GeometrySummary=>string.Join(" · ",new[]{LengthMm is null?null:"L "+LengthMm.Value.ToString("0.##")+" mm",RadiusMm is null?null:"R "+RadiusMm.Value.ToString("0.##")+" mm",AngleDeg is null?null:AngleDeg.Value.ToString("0.##")+"°"}.Where(x=>x is not null));
    public IReadOnlyList<CataloguePort> Ports
    {
        get
        {
            if(Ref is "20672" or "24671" or "24672" or "24771" or "24772")
            {var slim=Ref is "24771" or "24772";var left=Ref is "24671" or "24771";var direction=left?1d:-1d;var r1=slim?515d:360d;var r2=slim?579.3d:437.5d;const double a=Math.PI/6;return [new("IN",0,0,"estimated"),new("INNER",(decimal)(r1*Math.Sin(a)),(decimal)(direction*r1*(1-Math.Cos(a))),"estimated"),new("OUTER",(decimal)(r2*Math.Sin(a)),(decimal)(direction*r2*(1-Math.Cos(a))),"estimated")];}
            if(Ref=="24630"){var half=(LengthMm??188.3m)/2;var a=(double)(AngleDeg??24.3m)*Math.PI/180;var y=(decimal)((double)half*Math.Tan(a));return [new("IN",-half,0,"estimated"),new("STRAIGHT",half,0,"estimated"),new("LEFT",half,y,"estimated"),new("RIGHT",half,-y,"estimated")];}
            if(Ref is "24624" or "24640" or "24649" or "24720" or "24740"){var len=(double)(LengthMm??188.3m);var angle=(double)(AngleDeg??24.3m)*Math.PI/180;var h=len/2;var dx=h*Math.Cos(angle);var dy=h*Math.Sin(angle);return [new("A_IN",(decimal)-h,0,"derived"),new("A_OUT",(decimal)h,0,"derived"),new("B_IN",(decimal)-dx,(decimal)-dy,"derived"),new("B_OUT",(decimal)dx,(decimal)dy,"derived")];}
            if(Ref is "20611" or "20612" or "24611" or "24612" or "24711" or "24712"){var len=LengthMm??(Ref.StartsWith("247")?236.1m:188.3m);var angle=AngleDeg??(Ref.StartsWith("247")?12.1m:24.3m);var radius=RadiusMm??(Ref.StartsWith("247")?1114.6m:437.5m);var left=Ref is "20611" or "24611" or "24711";var half=len/2;var a=(double)angle*Math.PI/180;return [new("IN",-half,0,"estimated"),new("MAIN",half,0,"estimated"),new("BRANCH",(decimal)((double)radius*Math.Sin(a)),(decimal)((left?1:-1)*(double)radius*(1-Math.Cos(a))),"estimated")];}
            if(Type=="straight"||Ref is "20997" or "24088" or "24922" or "24951" or "24994" or "24995" or "24997"){var half=(LengthMm??188.3m)/2;return [new("A",-half,0,"derived"),new("B",half,0,"derived")];}
            if(RadiusMm is >0&&AngleDeg is >0&&Type=="curve"){var a=(double)AngleDeg.Value*Math.PI/360;var r=(double)RadiusMm.Value;var chord=(decimal)(r*Math.Sin(a));var sag=(decimal)(r*(1-Math.Cos(a)));return [new("A",-chord,-sag/2,"derived"),new("B",chord,sag/2,"derived")];}
            if(Ref is "74861" or "74862"){var ids=new[]{"N","NE","SE","S","SW","NW"};var angles=new[]{-90d,-30d,30d,90d,150d,210d};return ids.Select((id,i)=>new CataloguePort(id,(decimal)(131.5*Math.Cos(angles[i]*Math.PI/180)),(decimal)(131.5*Math.Sin(angles[i]*Math.PI/180)),"estimated")).ToArray();}
            return [];
        }
    }
    public IReadOnlyList<CatalogueTrackPath> TrackPaths
    {
        get
        {
            if(Ref is "20672" or "24671" or "24672" or "24771" or "24772"){var slim=Ref is "24771" or "24772";var r1=slim?515m:360m;var r2=slim?579.3m:437.5m;return [new("IN","INNER","quadratic",r1*(decimal)Math.Tan(Math.PI/12),0),new("IN","OUTER","quadratic",r2*(decimal)Math.Tan(Math.PI/12),0)];}
            if(Ref=="24630")return [new("IN","STRAIGHT","line"),new("IN","LEFT","quadratic"),new("IN","RIGHT","quadratic")];
            if(Ref is "24624" or "24640" or "24649" or "24720" or "24740")return [new("A_IN","A_OUT","line"),new("B_IN","B_OUT","line")];
            if(Ref is "20611" or "20612" or "24611" or "24612" or "24711" or "24712")return [new("IN","MAIN","line"),new("IN","BRANCH","quadratic")];
            if(Ports.Count==2)return [new(Ports[0].Id,Ports[1].Id,Type=="curve"?"quadratic":"line",0,(decimal)(-(double)(RadiusMm??0m)*(1-Math.Cos((double)(AngleDeg??0m)*Math.PI/360))))];
            return [];
        }
    }
}
public sealed record CatalogueDocument(string CatalogueVersion,string Brand,string System,string Scale,List<CatalogueItem> Items);
public static class MarklinCCatalogue
{
    public static CatalogueDocument Load(string path){using var stream=File.OpenRead(path);return JsonSerializer.Deserialize<CatalogueDocument>(stream,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidDataException("Catàleg Märklin C invàlid.");}
}