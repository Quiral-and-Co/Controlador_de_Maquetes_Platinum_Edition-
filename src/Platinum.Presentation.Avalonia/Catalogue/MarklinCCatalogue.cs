
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
public sealed record CatalogueDocument(string CatalogueVersion,string Brand,string System,string Scale,IReadOnlyList<CatalogueItem> Items);

public static class MarklinCCatalogue
{
    public static CatalogueDocument Load() => new(
        "2026.11-expanded", "Märklin", "C-Gleis", "H0",
        new CatalogueItem[]
        {
        new("20099", "Conjunt de rectes Start up", "conjunt", "special", null, null, null, "unverified", "Conjunt de peces, no és una peça única; inventari del paquet pendent de modelatge com a composició."),
        new("20130", "Corba R1 · 30° (Start up)", "corba", "curve", null, 360, 30, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20172", "Recta 171,7 mm (Start up)", "recta", "straight", 171.7, null, null, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20188", "Recta 188,3 mm (Start up)", "recta", "straight", 188.3, null, null, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20196", "Corba R1 · 30° transparent", "corba", "curve", null, 360, 30, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20197", "Recta 188,3 mm transparent", "recta", "straight", 188.3, null, null, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20199", "Peça final transparent", "especial", "special", null, null, null, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20224", "Corba R2 · 24,3°", "corba", "curve", null, 437.5, 24.3, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20230", "Corba R2 · 30°", "corba", "curve", null, 437.5, 30, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20299", "Conjunt de corbes Start up", "conjunt", "special", null, null, null, "unverified", "Conjunt de peces, no és una peça única; inventari del paquet pendent de modelatge com a composició."),
        new("20330", "Corba R3 · 30°", "corba", "curve", null, 515, 30, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20360", "Recta 360 mm (Start up)", "recta", "straight", 360, null, null, "confirmed", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20611", "Desviament esquerre R2 (Start up)", "desviament", "turnout", 188.3, 437.5, 24.3, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20612", "Desviament dret R2 (Start up)", "desviament", "turnout", 188.3, 437.5, 24.3, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20672", "Desviament corb dret R1/R2 (Start up)", "desviament corb", "turnout", null, 360, 30, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20977", "Topall de via (Start up)", "especial", "special", null, null, null, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("20997", "Via desenganxadora Start up · 94,2 mm", "especial", "special", 94.2, null, null, "partial", "Longitud nominal; geometria detallada de la via funcional pendent de verificació."),
        new("24001", "Peça final de bancada", "especial", "special", null, null, null, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24064", "Recta 64,3 mm", "recta", "straight", 64.3, null, null, "confirmed", null),
        new("24071", "Recta 70,8 mm", "recta", "straight", 70.8, null, null, "confirmed", "Bancada desmuntable segons documentació de catàleg."),
        new("24077", "Recta 77,5 mm", "recta", "straight", 77.5, null, null, "confirmed", null),
        new("24088", "Via d’alimentació · 188,3 mm", "especial", "special", 188.3, null, null, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24094", "Recta 94,2 mm", "recta", "straight", 94.2, null, null, "confirmed", null),
        new("24107", "Corba R1 · 7,5°", "corba", "curve", null, 360, 7.5, "confirmed", null),
        new("24115", "Corba R1 · 15°", "corba", "curve", null, 360, 15, "confirmed", null),
        new("24130", "Corba R1 · 30°", "corba", "curve", null, 360, 30, "confirmed", null),
        new("24172", "Recta 171,7 mm", "recta", "straight", 171.7, null, null, "confirmed", null),
        new("24188", "Recta 188,3 mm", "recta", "straight", 188.3, null, null, "confirmed", null),
        new("24194", "Via de commutació corba R1 · 15°", "via de contacte", "curve", null, 360, 15, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24206", "Corba R2 · 5,7°", "corba", "curve", null, 437.5, 5.7, "confirmed", "Radi i angle nominal identificats en fitxa de producte."),
        new("24207", "Corba R2 · 7,5°", "corba", "curve", null, 437.5, 7.5, "confirmed", null),
        new("24215", "Corba R2 · 15°", "corba", "curve", null, 437.5, 15, "confirmed", null),
        new("24224", "Contracorb R2 per a desviaments", "complement", "curve", null, 437.5, null, "partial", "Radi documentat; angle exacte i geometria de connexió pendent de verificació."),
        new("24229", "Recta 229,3 mm", "recta", "straight", 229.3, null, null, "confirmed", null),
        new("24230", "Corba R2 · 30°", "corba", "curve", null, 437.5, 30, "confirmed", null),
        new("24236", "Recta 236,1 mm", "recta", "straight", 236.1, null, null, "confirmed", null),
        new("24294", "Via de commutació corba R2 · 15°", "via de contacte", "curve", null, 437.5, 15, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24315", "Corba R3 · 15°", "corba", "curve", null, 515, 15, "confirmed", null),
        new("24330", "Corba R3 · 30°", "corba", "curve", null, 515, 30, "confirmed", null),
        new("24360", "Recta 360 mm", "recta", "straight", 360, null, null, "confirmed", null),
        new("24430", "Corba R4 · 30°", "corba", "curve", null, 579.3, 30, "confirmed", null),
        new("24530", "Corba R5 · 30°", "corba", "curve", null, 643.6, 30, "confirmed", null),
        new("24611", "Desviament esquerre R2", "desviament", "turnout", 188.3, 437.5, null, "partial", "Longitud nominal i radi de branca documentats; angle exacte i punts de connexió pendents de fitxa tècnica."),
        new("24612", "Desviament dret R2", "desviament", "turnout", 188.3, 437.5, null, "partial", "Longitud nominal i radi de branca documentats; angle exacte i punts de connexió pendents de fitxa tècnica."),
        new("24624", "Desviament doble (DKW) R2", "desviament", "turnout", 188.3, 437.5, 24.3, "unverified", "Geometria de ports pendent de verificació; es representa com a símbol esquemàtic."),
        new("24630", "Desviament triple R2", "desviament", "turnout", 188.3, 437.5, 24.3, "partial", "Longitud nominal i radi documentats; geometria de ramals pendent de fitxa tècnica."),
        new("24640", "Creuament 24,3°", "creuament", "special", 188.3, null, 24.3, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24649", "Creuament 48,6°", "creuament", "special", 103.3, null, 48.6, "partial", "Angle nominal conegut; longitud i coordenades dels ports pendents de verificació."),
        new("24671", "Desviament corb esquerre R1/R2", "desviament corb", "turnout", null, 360, 30, "unverified", "Geometria detallada pendent de fitxa tècnica."),
        new("24672", "Desviament corb dret R1/R2", "desviament corb", "turnout", null, 360, 30, "unverified", "Geometria detallada pendent de fitxa tècnica."),
        new("24711", "Desviament esvelt esquerre R9 · 12,1°", "desviament", "turnout", null, 1114.5, 12.1, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24712", "Desviament esvelt dret R9 · 12,1°", "desviament", "turnout", null, 1114.5, 12.1, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24720", "Desviament doble esvelt · 12,1°", "desviament", "turnout", null, null, 12.1, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24740", "Creuament esvelt · 12,1°", "creuament", "special", 236.1, null, 12.1, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24771", "Desviament corb esvelt esquerre R3/R4", "desviament corb", "turnout", null, 515, 30, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24772", "Desviament corb esvelt dret R3/R4", "desviament corb", "turnout", null, 515, 30, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24912", "Corba R9 · 12,1° (contracorb de desviament esvelt)", "corba", "curve", null, 1114.5, 12.1, "partial", "Referència R9 i angle nominal; cal verificar punts de connexió exactes."),
        new("24922", "Transició C a K · 180 mm", "transició", "special", 180, null, null, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24951", "Transició C a M · 180 mm", "transició", "special", 180, null, null, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24977", "Topall de via · 77,5 mm", "especial", "special", 77.5, null, null, "partial", null),
        new("24978", "Topall il·luminat · 77,5 mm", "especial", "special", 77.5, null, null, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24994", "Via de commutació recta · 94,2 mm", "via de contacte", "special", 94.2, null, null, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24995", "Via de contacte recta · 94,2 mm", "via de contacte", "special", 94.2, null, null, "partial", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("24997", "Via desenganxadora · 94,2 mm", "especial", "special", 94.2, null, null, "partial", null),
        new("74861", "Placa giratòria C-Gleis", "especial", "special", null, null, null, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("74862", "Placa giratòria C-Gleis · nova versió", "especial", "special", null, null, null, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("74920", "Pas a nivell amb barreres", "especial", "special", null, null, null, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui."),
        new("74924", "Pas a nivell Start up", "especial", "special", null, null, null, "unverified", "Fitxa geomètrica completa pendent de verificació; representació esquemàtica quan correspongui.")
        });
}
