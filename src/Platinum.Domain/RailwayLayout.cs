namespace Platinum.Domain;
public enum OccupancyState { Unknown, Clear, Occupied }
public sealed record TrackBlock(Guid Id, string Name, int Level, OccupancyState Occupancy);
public sealed record Route(Guid Id, string Name, IReadOnlyList<Guid> BlockIds);
public sealed class Layout
{
    private readonly Dictionary<Guid, TrackBlock> _blocks = [];
    private readonly Dictionary<Guid, Route> _routes = [];
    private readonly Dictionary<Guid, Guid> _reservations = [];
    public IReadOnlyCollection<TrackBlock> Blocks => _blocks.Values;
    public IReadOnlyCollection<Route> Routes => _routes.Values;
    public IReadOnlyDictionary<Guid, Guid> Reservations => _reservations;
    public void AddBlock(TrackBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (!_blocks.TryAdd(block.Id, block)) throw new InvalidOperationException("El bloc ja existeix.");
    }
    public void AddRoute(Route route)
    {
        ArgumentNullException.ThrowIfNull(route);
        if (route.BlockIds.Count == 0 || route.BlockIds.Distinct().Count() != route.BlockIds.Count)
            throw new ArgumentException("L'itinerari ha de contenir blocs únics.", nameof(route));
        if (route.BlockIds.Any(id => !_blocks.ContainsKey(id)))
            throw new ArgumentException("L'itinerari conté blocs inexistents.", nameof(route));
        if (!_routes.TryAdd(route.Id, route)) throw new InvalidOperationException("L'itinerari ja existeix.");
    }
    public bool TryReserve(Guid routeId)
    {
        if (!_routes.TryGetValue(routeId, out var route)) return false;
        if (route.BlockIds.Any(id => _blocks[id].Occupancy != OccupancyState.Clear || _reservations.ContainsKey(id))) return false;
        foreach (var id in route.BlockIds) _reservations.Add(id, routeId);
        return true;
    }
    public void SetOccupancy(Guid blockId, OccupancyState state)
    {
        if (!_blocks.TryGetValue(blockId, out var block)) throw new KeyNotFoundException("Bloc inexistent.");
        _blocks[blockId] = block with { Occupancy = state };
        if (state != OccupancyState.Clear && _reservations.TryGetValue(blockId, out var routeId)) ReleaseRoute(routeId);
    }
    public void ReleaseRoute(Guid routeId)
    {
        foreach (var id in _reservations.Where(x => x.Value == routeId).Select(x => x.Key).ToArray()) _reservations.Remove(id);
    }
}