using System.Text.Json;
namespace Platinum.Presentation.Avalonia.Catalogue;
public sealed record CatalogueItem(string Ref,string Name,string Category,string Type,decimal? LengthMm,decimal? RadiusMm,decimal? AngleDeg,string GeometryStatus,string? Notes)
{
    public string DisplayName => Ref + " · " + Name;
    public string GeometrySummary => string.Join(" · ", new[] {
        LengthMm is null ? null : "L " + LengthMm.Value.ToString("0.##") + " mm",
        RadiusMm is null ? null : "R " + RadiusMm.Value.ToString("0.##") + " mm",
        AngleDeg is null ? null : AngleDeg.Value.ToString("0.##") + "°"
    }.Where(x => x is not null));
}
public sealed record CatalogueDocument(string CatalogueVersion,string Brand,string System,string Scale,List<CatalogueItem> Items);
public static class MarklinCCatalogue
{
    public static CatalogueDocument Load(string path)
    {
        using var stream=File.OpenRead(path);
        return JsonSerializer.Deserialize<CatalogueDocument>(stream,new JsonSerializerOptions { PropertyNameCaseInsensitive=true })
            ?? throw new InvalidDataException("El catàleg Märklin C és buit o no té un format vàlid.");
    }
}