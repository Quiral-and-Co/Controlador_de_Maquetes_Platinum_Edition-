namespace Platinum.Domain.Layout;

/// <summary>Estat d'ocupació lògic; no representa una lectura física certificada.</summary>
public enum BlockOccupancyState
{
    Unknown = 0,
    Free = 1,
    Occupied = 2
}

public sealed record BlockOccupancy(Guid BlockId, BlockOccupancyState State);

/// <summary>Reserva lògica activa d'un itinerari dins del projecte.</summary>
public sealed record RouteReservation(Guid Id, Guid RouteId, DateTimeOffset CreatedUtc);
