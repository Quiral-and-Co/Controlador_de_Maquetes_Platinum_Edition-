using Platinum.Domain;
namespace Platinum.Application;
public sealed class LayoutUseCases(Layout layout)
{
    public Layout Layout { get; } = layout ?? throw new ArgumentNullException(nameof(layout));
    public bool ReserveRoute(Guid routeId) => Layout.TryReserve(routeId);
    public void ReportOccupancy(Guid blockId, OccupancyState state) => Layout.SetOccupancy(blockId, state);
}