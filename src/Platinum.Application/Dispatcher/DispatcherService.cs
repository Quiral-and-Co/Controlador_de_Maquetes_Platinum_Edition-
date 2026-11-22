using Platinum.Domain.Layout;

namespace Platinum.Application.Dispatcher;

/// <summary>
/// Dispatcher local sobre el TrackLayout actiu. No disposa d'adaptadors de maquinari
/// i mai no envia ordres a locomotores, desviaments o senyals.
/// </summary>
public sealed class DispatcherService
{
    public void Initialize(TrackLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var blockIds = layout.Blocks.Select(x => x.Id).ToHashSet();
        layout.BlockOccupancies.RemoveAll(x => !blockIds.Contains(x.BlockId));
        foreach (var block in layout.Blocks)
            if (layout.BlockOccupancies.All(x => x.BlockId != block.Id))
                layout.BlockOccupancies.Add(new BlockOccupancy(block.Id, BlockOccupancyState.Unknown));
        var routeIds = layout.Routes.Select(x => x.Id).ToHashSet();
        layout.RouteReservations.RemoveAll(x => !routeIds.Contains(x.RouteId));
    }

    public void SetOccupancy(TrackLayout layout, Guid blockId, BlockOccupancyState state)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        if (layout.Blocks.All(x => x.Id != blockId))
            throw new InvalidOperationException("El bloc no existeix al TrackLayout actiu.");

        var current = layout.BlockOccupancies.FindIndex(x => x.BlockId == blockId);
        var value = new BlockOccupancy(blockId, state);
        if (current < 0) layout.BlockOccupancies.Add(value);
        else layout.BlockOccupancies[current] = value;

        // Un bloc que deixa d'estar lliure invalida les reserves lògiques que el contenen.
        if (state != BlockOccupancyState.Free)
        {
            var affectedRouteIds = layout.Routes
                .Where(route => route.BlockIds.Contains(blockId))
                .Select(route => route.Id)
                .ToHashSet();
            layout.RouteReservations.RemoveAll(x => affectedRouteIds.Contains(x.RouteId));
        }
    }

    public RouteDefinition CreateRoute(TrackLayout layout, string name, IEnumerable<Guid> blockIds)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("L'itinerari necessita un nom.", nameof(name));
        var ids = blockIds?.Distinct().ToList() ?? throw new ArgumentNullException(nameof(blockIds));
        if (ids.Count == 0) throw new InvalidOperationException("Selecciona almenys un bloc.");
        var known = layout.Blocks.Select(x => x.Id).ToHashSet();
        if (ids.Any(id => !known.Contains(id)))
            throw new InvalidOperationException("L'itinerari conté blocs que no pertanyen al TrackLayout actiu.");
        var route = new RouteDefinition(Guid.NewGuid(), name.Trim(), ids);
        layout.Routes.Add(route);
        return route;
    }

    public bool TryReserveRoute(TrackLayout layout, Guid routeId, out string reason)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var route = layout.Routes.FirstOrDefault(x => x.Id == routeId);
        if (route is null) { reason = "L'itinerari no existeix."; return false; }
        if (layout.RouteReservations.Any(x => x.RouteId == routeId))
        { reason = "Aquest itinerari ja està reservat."; return false; }

        foreach (var blockId in route.BlockIds)
        {
            var occupancy = layout.BlockOccupancies.FirstOrDefault(x => x.BlockId == blockId)?.State
                ?? BlockOccupancyState.Unknown;
            if (occupancy != BlockOccupancyState.Free)
            {
                var blockName = layout.Blocks.FirstOrDefault(x => x.Id == blockId)?.Name ?? blockId.ToString();
                reason = $"No es pot reservar: el bloc «{blockName}» no està lliure.";
                return false;
            }
            if (layout.RouteReservations.Any(reservation =>
                layout.Routes.Any(other => other.Id == reservation.RouteId && other.BlockIds.Contains(blockId))))
            {
                var blockName = layout.Blocks.FirstOrDefault(x => x.Id == blockId)?.Name ?? blockId.ToString();
                reason = $"Conflicte: el bloc «{blockName}» ja està reservat per un altre itinerari.";
                return false;
            }
        }

        layout.RouteReservations.Add(new RouteReservation(Guid.NewGuid(), routeId, DateTimeOffset.UtcNow));
        reason = "Reserva lògica creada. No s'ha enviat cap ordre física.";
        return true;
    }

    public bool ReleaseRoute(TrackLayout layout, Guid routeId)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return layout.RouteReservations.RemoveAll(x => x.RouteId == routeId) > 0;
    }
}
