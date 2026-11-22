using System.Text.Json;

namespace Platinum.Presentation.Wpf.Catalogue;

public sealed record CataloguePort(string Id, decimal X, decimal Y, string Accuracy);

public sealed record CatalogueItem(
    string Ref,
    string Name,
    string Category,
    string Type,
    decimal? LengthMm,
    decimal? RadiusMm,
    decimal? AngleDeg,
    string GeometryStatus,
    string? Notes)
{
    public string DisplayName => $"{Ref} · {Name}";
    public string GeometrySummary => string.Join(" · ", new[]
    {
        LengthMm is null ? null : $"L {LengthMm:0.##} mm",
        RadiusMm is null ? null : $"R {RadiusMm:0.##} mm",
        AngleDeg is null ? null : $"{AngleDeg:0.##}°"
    }.Where(x => x is not null));

    /// <summary>
    /// Connection points in millimetres relative to the placed item's origin.
    /// Straight and circular-track ports are derived from catalogue dimensions.
    /// Turnout branch coordinates are estimates until checked against a Märklin
    /// dimension drawing; they must never be treated as physically verified.
    /// </summary>
    public IReadOnlyList<CataloguePort> Ports
    {
        get
        {
            if (LengthMm is > 0 && Type == "straight")
            {
                var half = LengthMm.Value / 2m;
                return [new("A", -half, 0m, "derived"), new("B", half, 0m, "derived")];
            }

            if (RadiusMm is > 0 && AngleDeg is > 0 && Type == "curve")
            {
                var halfAngle = (double)AngleDeg.Value * Math.PI / 360d;
                var radius = (double)RadiusMm.Value;
                var halfChord = (decimal)(radius * Math.Sin(halfAngle));
                var sagitta = (decimal)(radius * (1d - Math.Cos(halfAngle)));
                return [
                    new("A", -halfChord, sagitta / 2m, "derived"),
                    new("B", halfChord, -sagitta / 2m, "derived")
                ];
            }

            if (LengthMm is > 0 && AngleDeg is > 0 && Type == "turnout")
            {
                var half = LengthMm.Value / 2m;
                var radians = (double)AngleDeg.Value * Math.PI / 180d;
                var branchY = (decimal)(half * Math.Sin(radians));
                var branchX = (decimal)(half * Math.Cos(radians));
                return [
                    new("IN", -half, 0m, "derived"),
                    new("MAIN", half, 0m, "derived"),
                    new("BRANCH", branchX, branchY, "estimated")
                ];
            }

            return [];
        }
    }
}

public sealed record CatalogueDocument(string CatalogueVersion, string Brand, string System, string Scale, List<CatalogueItem> Items);

public static class MarklinCCatalogue
{
    public static CatalogueDocument Load(string path)
    {
        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<CatalogueDocument>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return document ?? throw new InvalidDataException("El catàleg Märklin C és buit o no té un format vàlid.");
    }
}
