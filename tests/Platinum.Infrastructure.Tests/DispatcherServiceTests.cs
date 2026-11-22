using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Platinum.Infrastructure.Projects;
using Platinum.Application.Dispatcher;
using Platinum.Domain.Layout;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class DispatcherServiceTests
{
    private static (TrackLayout Layout, Guid First, Guid Second, Guid Level) CreateLayout()
    {
        var level = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var layout = new TrackLayout
        {
            Name = "Dispatcher test",
            Levels = new List<TrackLevel> { new(level, "Nivell 1", 0m) },
            Blocks = new List<LogicalBlock>
            {
                new(first, "Bloc A", level, Array.Empty<Guid>()),
                new(second, "Bloc B", level, Array.Empty<Guid>())
            }
        };
        return (layout, first, second, level);
    }

    [Fact]
    public void Initialize_CreatesUnknownOccupancyForEachBlock()
    {
        var (layout, first, second, _) = CreateLayout();
        new DispatcherService().Initialize(layout);

        Assert.Equal(2, layout.BlockOccupancies.Count);
        Assert.All(layout.BlockOccupancies, item => Assert.Equal(BlockOccupancyState.Unknown, item.State));
        Assert.Contains(layout.BlockOccupancies, item => item.BlockId == first);
        Assert.Contains(layout.BlockOccupancies, item => item.BlockId == second);
    }

    [Fact]
    public void Reservation_IsRejectedUntilEveryBlockIsFree()
    {
        var (layout, first, second, _) = CreateLayout();
        var service = new DispatcherService();
        service.Initialize(layout);
        var route = service.CreateRoute(layout, "A-B", new[] { first, second });

        Assert.False(service.TryReserveRoute(layout, route.Id, out _));
        service.SetOccupancy(layout, first, BlockOccupancyState.Free);
        service.SetOccupancy(layout, second, BlockOccupancyState.Free);
        Assert.True(service.TryReserveRoute(layout, route.Id, out _));
        Assert.Single(layout.RouteReservations);
    }

    [Fact]
    public void SharedBlock_CannotBeReservedByTwoRoutes()
    {
        var (layout, first, second, _) = CreateLayout();
        var service = new DispatcherService();
        service.Initialize(layout);
        service.SetOccupancy(layout, first, BlockOccupancyState.Free);
        service.SetOccupancy(layout, second, BlockOccupancyState.Free);
        var routeA = service.CreateRoute(layout, "Ruta A", new[] { first });
        var routeB = service.CreateRoute(layout, "Ruta B", new[] { first, second });

        Assert.True(service.TryReserveRoute(layout, routeA.Id, out _));
        Assert.False(service.TryReserveRoute(layout, routeB.Id, out _));
    }

    [Fact]
    public async Task PlatinumPackage_PersistsOccupancyRoutesAndReservations()
    {
        var (layout, first, _, _) = CreateLayout();
        var dispatcher = new DispatcherService();
        dispatcher.Initialize(layout);
        dispatcher.SetOccupancy(layout, first, BlockOccupancyState.Free);
        var route = dispatcher.CreateRoute(layout, "Ruta persistent", new[] { first });
        Assert.True(dispatcher.TryReserveRoute(layout, route.Id, out _));

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".platinum");
        try
        {
            var store = new PlatinumPackageStore();
            await store.SaveAsync(layout, path);
            var loaded = await store.LoadAsync(path);

            Assert.Contains(loaded.BlockOccupancies, item => item.BlockId == first && item.State == BlockOccupancyState.Free);
            Assert.Contains(loaded.Routes, item => item.Id == route.Id);
            Assert.Contains(loaded.RouteReservations, item => item.RouteId == route.Id);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void OccupyingReservedBlock_ReleasesAffectedRouteReservation()
    {
        var (layout, first, _, _) = CreateLayout();
        var service = new DispatcherService();
        service.Initialize(layout);
        service.SetOccupancy(layout, first, BlockOccupancyState.Free);
        var route = service.CreateRoute(layout, "Ruta A", new[] { first });
        Assert.True(service.TryReserveRoute(layout, route.Id, out _));

        service.SetOccupancy(layout, first, BlockOccupancyState.Occupied);

        Assert.Empty(layout.RouteReservations);
    }
}
