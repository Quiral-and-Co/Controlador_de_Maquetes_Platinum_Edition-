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
    /// Local endpoint coordinates in millimetres. "derived" means calculated
    /// from explicit catalogue dimensions. "estimated" means topology is known
    /// but endpoint coordinates still require a dimensioned drawing or measurement.
    /// </summary>
    public IReadOnlyList<CataloguePort> Ports
    {
        get
        {
            if (Ref is "24671" or "24672" or "24771" or "24772")
                return CurvedTurnoutPorts();

            if (Ref == "24630")
                return ThreeWayTurnoutPorts();

            if (Ref is "24640" or "24649" or "24740" or "24624" or "24720")
                return CrossingPorts();

            if (Ref is "20611" or "20612" or "24611" or "24612" or "24711" or "24712")
                return StandardTurnoutPorts();

            if (LengthMm is > 0 && (Type == "straight" || Ref is "24088" or "24922" or "24951" or "24994" or "24995" or "24997" or "20997"))
            {
                var half = LengthMm.Value / 2m;
                return [new("A", -half, 0m, "derived"), new("B", half, 0m, "derived")];
            }

            if (RadiusMm is > 0 && AngleDeg is > 0 && Type == "curve")
                return ArcPorts(RadiusMm.Value, AngleDeg.Value, "A", "B", "derived");

            if (Ref is "20977" or "24977" or "24978" or "20199" or "24001")
                return [new("IN", -((LengthMm ?? 77.5m) / 2m), 0m, "estimated")];

            // Turntables and packaged track sets require a radial/composite topology,
            // so no artificial two-port geometry is assigned.
            return [];
        }
    }

    private IReadOnlyList<CataloguePort> CurvedTurnoutPorts()
    {
        var innerRadius = Ref is "24771" or "24772" ? 515m : 360m;
        var outerRadius = Ref is "24771" or "24772" ? 579.3m : 437.5m;
        var leftHand = Ref is "24671" or "24771";
        var direction = leftHand ? 1m : -1m;
        const decimal nominalAngle = 30m;
        var halfAngle = (double)nominalAngle * Math.PI / 360d;
        var meanRadius = (innerRadius + outerRadius) / 2m;
        var ports = new List<CataloguePort>(4);

        AddArcPair(ports, "INNER", innerRadius, meanRadius, halfAngle, direction);
        AddArcPair(ports, "OUTER", outerRadius, meanRadius, halfAngle, direction);
        return ports;
    }

    private static void AddArcPair(
        ICollection<CataloguePort> ports,
        string prefix,
        decimal radius,
        decimal meanRadius,
        double halfAngle,
        decimal direction)
    {
        var r = (double)radius;
        var mean = (double)meanRadius;
        var x = (decimal)(r * Math.Sin(halfAngle));
        var y = (decimal)((r - mean) * Math.Cos(halfAngle)) * direction;
        ports.Add(new CataloguePort($"{prefix}_A", -x, y, "estimated"));
        ports.Add(new CataloguePort($"{prefix}_B", x, y, "estimated"));
    }

    private IReadOnlyList<CataloguePort> ThreeWayTurnoutPorts()
    {
        // Topology: one common toe and three branch exits. The catalogue data
        // does not yet contain all moulded endpoint offsets, so these are estimates.
        const decimal halfLength = 188.3m / 2m;
        var branchOffset = (decimal)(188.3d * Math.Sin(24.3d * Math.PI / 180d));
        return [
            new("IN", -halfLength, 0m, "estimated"),
            new("STRAIGHT", halfLength, 0m, "estimated"),
            new("LEFT", halfLength, branchOffset, "estimated"),
            new("RIGHT", halfLength, -branchOffset, "estimated")
        ];
    }

    private IReadOnlyList<CataloguePort> StandardTurnoutPorts()
    {
        var length = Ref is "24711" or "24712" ? 236.1m : 188.3m;
        var angle = AngleDeg ?? 24.3m;
        var radius = RadiusMm ?? 437.5m;
        var leftHand = Ref is "20611" or "24611" or "24711";
        var direction = leftHand ? 1d : -1d;
        var half = length / 2m;
        var halfAngle = (double)angle * Math.PI / 360d;
        var branchX = (decimal)(length * Math.Cos(halfAngle) / 2d);
        var branchY = (decimal)(direction * radius * (1d - Math.Cos((double)angle * Math.PI / 180d)));
        return [
            new("IN", -half, 0m, "estimated"),
            new("MAIN", half, 0m, "estimated"),
            new("BRANCH", branchX, branchY, "estimated")
        ];
    }

    private IReadOnlyList<CataloguePort> CrossingPorts()
    {
        // Crossing angle is catalogued, but exact overall length/track-centre
        // coordinates are not consistently present in this catalogue yet.
        // Use a provisional nominal envelope and mark all four endpoints estimated.
        var angle = AngleDeg ?? (Ref == "24649" ? 48.6m : Ref is "24740" or "24720" ? 12.1m : 24.3m);
        var length = LengthMm ?? 188.3m;
        var half = (double)length / 2d;
        var radians = (double)angle * Math.PI / 180d;
        var dx = half * Math.Cos(radians);
        var dy = half * Math.Sin(radians);
        return [
            new("A_IN", (decimal)-half, 0m, "estimated"),
            new("A_OUT", (decimal)half, 0m, "estimated"),
            new("B_IN", (decimal)-dx, (decimal)-dy, "estimated"),
            new("B_OUT", (decimal)dx, (decimal)dy, "estimated")
        ];
    }

    private static IReadOnlyList<CataloguePort> ArcPorts(
        decimal radius,
        decimal angle,
        string startId,
        string endId,
        string accuracy)
    {
        var halfAngle = (double)angle * Math.PI / 360d;
        var r = (double)radius;
        var halfChord = (decimal)(r * Math.Sin(halfAngle));
        var sagitta = (decimal)(r * (1d - Math.Cos(halfAngle)));
        return [
            new(startId, -halfChord, -sagitta / 2m, accuracy),
            new(endId, halfChord, sagitta / 2m, accuracy)
        ];
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
