namespace Platinum.Domain.Catalogue;

public sealed record CataloguePort(string Id, decimal X, decimal Y, string Accuracy);

/// <summary>One centre-line route between two catalogue ports, in local millimetres.</summary>
public sealed record CatalogueTrackPath(
    string FromPort,
    string ToPort,
    string Kind,
    decimal ControlX = 0m,
    decimal ControlY = 0m);

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
    /// Connection endpoints in local millimetres. Derived coordinates use published
    /// dimensions. Estimated coordinates describe a known topology where Märklin's
    /// public dimensional data does not specify the exact moulded connector centres.
    /// </summary>
    public IReadOnlyList<CataloguePort> Ports
    {
        get
        {
            if (IsCurvedTurnout)
                return CurvedTurnoutPorts();

            if (Ref == "24630")
                return ThreeWayTurnoutPorts();

            if (IsCrossingOrDoubleSlip)
                return CrossingPorts();

            if (IsStandardTurnout)
                return StandardTurnoutPorts();

            if (IsKnownStraightSpecial)
                return StraightPorts();

            if (RadiusMm is > 0 && AngleDeg is > 0 && Type == "curve")
                return ArcPorts(RadiusMm.Value, AngleDeg.Value, "A", "B", "derived");

            if (Ref is "20977" or "24977" or "24978" or "20199" or "24001")
                return [new("IN", -((LengthMm ?? 77.5m) / 2m), 0m, "estimated")];

            if (Ref is "74861" or "74862")
                return TurntablePorts();

            // Sets are compositions, not individual track pieces.
            return [];
        }
    }

    /// <summary>
    /// Centre-line routes for drawing. Every route references the same port IDs
    /// used by connection hit-testing, so rendered endpoints and snap points coincide.
    /// </summary>
    public IReadOnlyList<CatalogueTrackPath> TrackPaths
    {
        get
        {
            if (IsCurvedTurnout)
            {
                var angle = 30d * Math.PI / 180d;
                var innerRadius = Ref is "24771" or "24772" ? 515m : 360m;
                var outerRadius = Ref is "24771" or "24772" ? 579.3m : 437.5m;
                return [
                    new("IN", "INNER", "quadratic", (decimal)((double)innerRadius * Math.Tan(angle / 2d)), 0m),
                    new("IN", "OUTER", "quadratic", (decimal)((double)outerRadius * Math.Tan(angle / 2d)), 0m)
                ];
            }

            if (Ref == "24630")
                return [
                    new("IN", "STRAIGHT", "line"),
                    new("IN", "LEFT", "quadratic", 0m, 0m),
                    new("IN", "RIGHT", "quadratic", 0m, 0m)
                ];

            if (IsCrossingOrDoubleSlip)
                return [
                    new("A_IN", "A_OUT", "line"),
                    new("B_IN", "B_OUT", "line")
                ];

            if (IsStandardTurnout)
                return [
                    new("IN", "MAIN", "line"),
                    new("IN", "BRANCH", "quadratic", 0m, 0m)
                ];

            if (IsKnownStraightSpecial)
                return [new("A", "B", "line")];

            if (RadiusMm is > 0 && AngleDeg is > 0 && Type == "curve")
            {
                var half = (double)AngleDeg.Value * Math.PI / 360d;
                var radius = (double)RadiusMm.Value;
                return [new("A", "B", "quadratic", 0m, (decimal)(-radius * (1d - Math.Cos(half))))];
            }

            if (Ref is "74861" or "74862")
                return [];

            return [];
        }
    }

    private bool IsCurvedTurnout =>
        Ref is "20672" or "24671" or "24672" or "24771" or "24772";

    private bool IsStandardTurnout =>
        Ref is "20611" or "20612" or "24611" or "24612" or "24711" or "24712";

    private bool IsCrossingOrDoubleSlip =>
        Ref is "24624" or "24640" or "24649" or "24720" or "24740";

    private bool IsKnownStraightSpecial =>
        Type == "straight" ||
        Ref is "20997" or "24088" or "24922" or "24951" or "24994" or "24995" or "24997";

    private IReadOnlyList<CataloguePort> StraightPorts()
    {
        var length = LengthMm ?? Ref switch
        {
            "24922" or "24951" => 180m,
            "24994" or "24995" or "24997" or "20997" => 94.2m,
            _ => 188.3m
        };
        var half = length / 2m;
        return [new("A", -half, 0m, "derived"), new("B", half, 0m, "derived")];
    }

    private IReadOnlyList<CataloguePort> CurvedTurnoutPorts()
    {
        // A curved turnout is a three-ended junction: one approach and two exits.
        // It is not two independent concentric tracks (four ports).
        var slim = Ref is "24771" or "24772";
        var innerRadius = slim ? 515m : 360m;
        var outerRadius = slim ? 579.3m : 437.5m;
        var leftHand = Ref is "24671" or "24771";
        var direction = leftHand ? 1m : -1m;
        const double angle = 30d * Math.PI / 180d;
        var inner = ArcEndpoint(innerRadius, angle, direction);
        var outer = ArcEndpoint(outerRadius, angle, direction);
        return [
            new("IN", 0m, 0m, "estimated"),
            new("INNER", inner.X, inner.Y, "estimated"),
            new("OUTER", outer.X, outer.Y, "estimated")
        ];
    }

    private static (decimal X, decimal Y) ArcEndpoint(decimal radius, double angle, decimal direction)
    {
        var r = (double)radius;
        return ((decimal)(r * Math.Sin(angle)), (decimal)((double)direction * r * (1d - Math.Cos(angle))));
    }

    private IReadOnlyList<CataloguePort> ThreeWayTurnoutPorts()
    {
        // 24630: one common toe and three exits. Published envelope is
        // 188.3 mm with two 24.3-degree diverging routes. Exact frog offsets
        // and the asymmetric exit positions are not published in the catalogue.
        var halfLength = (LengthMm ?? 188.3m) / 2m;
        var angle = (double)(AngleDeg ?? 24.3m) * Math.PI / 180d;
        var branchX = halfLength;
        var branchY = (decimal)((double)halfLength * Math.Tan(angle));
        return [
            new("IN", -halfLength, 0m, "estimated"),
            new("STRAIGHT", halfLength, 0m, "estimated"),
            new("LEFT", branchX, branchY, "estimated"),
            new("RIGHT", branchX, -branchY, "estimated")
        ];
    }

    private IReadOnlyList<CataloguePort> StandardTurnoutPorts()
    {
        var length = LengthMm ?? (Ref is "24711" or "24712" ? 236.1m : 188.3m);
        var angle = AngleDeg ?? (Ref is "24711" or "24712" ? 12.1m : 24.3m);
        var radius = RadiusMm ?? (Ref is "24711" or "24712" ? 1114.6m : 437.5m);
        var leftHand = Ref is "20611" or "24611" or "24711";
        var direction = leftHand ? 1d : -1d;
        var half = length / 2m;
        var radians = (double)angle * Math.PI / 180d;
        var branchX = (decimal)((double)radius * Math.Sin(radians));
        var branchY = (decimal)(direction * (double)radius * (1d - Math.Cos(radians)));
        return [
            new("IN", -half, 0m, "estimated"),
            new("MAIN", half, 0m, "estimated"),
            new("BRANCH", branchX, branchY, "estimated")
        ];
    }

    private IReadOnlyList<CataloguePort> TurntablePorts()
    {
        // Six radial track sockets; angles are nominal and need drawing verification.
        const double radius = 131.5d;
        var ids = new[] { "N", "NE", "SE", "S", "SW", "NW" };
        var angles = new[] { -90d, -30d, 30d, 90d, 150d, 210d };
        return ids.Select((id, i) => new CataloguePort(id,
            (decimal)(radius * Math.Cos(angles[i] * Math.PI / 180d)),
            (decimal)(radius * Math.Sin(angles[i] * Math.PI / 180d)),
            "estimated")).ToArray();
    }

    private IReadOnlyList<CataloguePort> CrossingPorts()
    {
        var length = LengthMm ?? (Ref switch
        {
            "24649" => 103.3m,
            "24740" or "24720" => 236.1m,
            _ => 188.3m
        });
        var angle = AngleDeg ?? (Ref switch
        {
            "24649" => 48.6m,
            "24740" or "24720" => 12.1m,
            _ => 24.3m
        });
        if (length <= 0m || angle <= 0m)
            return [];

        // Each crossing road is a separate straight path. The four ends are
        // calculated from the documented overall length and crossing angle.
        var half = (double)length / 2d;
        var radians = (double)angle * Math.PI / 180d;
        var dx = half * Math.Cos(radians);
        var dy = half * Math.Sin(radians);
        return [
            new("A_IN", (decimal)-half, 0m, "derived"),
            new("A_OUT", (decimal)half, 0m, "derived"),
            new("B_IN", (decimal)-dx, (decimal)-dy, "derived"),
            new("B_OUT", (decimal)dx, (decimal)dy, "derived")
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

