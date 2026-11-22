using System.Windows;
using System.Windows.Controls;
using Platinum.Application.Dispatcher;
using Platinum.Domain.Layout;

namespace Platinum.Presentation.Wpf.Dispatcher;

public partial class DispatcherView : UserControl
{
    private readonly DispatcherService _service = new();
    private TrackLayout? _layout;

    public DispatcherView()
    {
        InitializeComponent();
    }

    public void SetLayout(TrackLayout layout)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _service.Initialize(_layout);
        BlockLevelSelector.ItemsSource = _layout.Levels;
        BlockLevelSelector.DisplayMemberPath = nameof(TrackLevel.Name);
        BlockLevelSelector.SelectedValuePath = nameof(TrackLevel.Id);
        BlockLevelSelector.SelectedValue = _layout.Levels.FirstOrDefault()?.Id;
        Refresh();
    }

    public void SyncToLayout()
    {
        if (_layout is not null) _service.Initialize(_layout);
    }

    private void Refresh()
    {
        if (_layout is null) return;
        var levels = _layout.Levels.ToDictionary(x => x.Id, x => x.Name);
        var rows = _layout.Blocks.Select(block =>
        {
            var state = _layout.BlockOccupancies.FirstOrDefault(x => x.BlockId == block.Id)?.State
                ?? BlockOccupancyState.Unknown;
            return new BlockRow(block.Id, block.Name, levels.GetValueOrDefault(block.LevelId, "Nivell desconegut"), state);
        }).ToList();
        BlocksGrid.ItemsSource = rows;
        BlockCountText.Text = $"{rows.Count} blocs · {_layout.Name}";
        RoutesList.ItemsSource = _layout.Routes.Select(route => new RouteRow(
            route.Id, route.Name,
            string.Join(" → ", route.BlockIds.Select(id => _layout.Blocks.FirstOrDefault(b => b.Id == id)?.Name ?? "?")),
            _layout.RouteReservations.Any(reservation => reservation.RouteId == route.Id))).ToList();
        ReservationsList.ItemsSource = _layout.RouteReservations.Select(reservation =>
        {
            var route = _layout.Routes.FirstOrDefault(x => x.Id == reservation.RouteId);
            return route is null ? "Itinerari desconegut" : $"{route.Name} · {reservation.CreatedUtc.ToLocalTime():g}";
        }).ToList();
        ReservationStatusText.Text = _layout.RouteReservations.Count == 0
            ? "Cap reserva activa."
            : $"{_layout.RouteReservations.Count} reserva(es) activa(es).";
    }

    private void AddBlock_Click(object sender, RoutedEventArgs e)
    {
        if (_layout is null) return;
        var name = BlockNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Escriu un nom per al bloc.", "Dispatcher", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (BlockLevelSelector.SelectedValue is not Guid levelId || _layout.Levels.All(x => x.Id != levelId))
        {
            MessageBox.Show("Selecciona un nivell vàlid.", "Dispatcher", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_layout.Blocks.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) && x.LevelId == levelId))
        {
            MessageBox.Show("Ja existeix un bloc amb aquest nom en el nivell seleccionat.", "Dispatcher", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _layout.Blocks.Add(new LogicalBlock(Guid.NewGuid(), name, levelId, Array.Empty<Guid>()));
        BlockNameBox.Clear();
        _service.Initialize(_layout);
        Refresh();
    }

    private void DeleteBlock_Click(object sender, RoutedEventArgs e)
    {
        if (_layout is null) return;
        var selected = BlocksGrid.SelectedItems.Cast<BlockRow>().Select(x => x.Id).ToHashSet();
        if (selected.Count == 0)
        {
            MessageBox.Show("Selecciona almenys un bloc per eliminar.", "Dispatcher", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var affectedRoutes = _layout.Routes.Where(route => route.BlockIds.Any(selected.Contains)).Select(route => route.Id).ToHashSet();
        _layout.RouteReservations.RemoveAll(reservation => affectedRoutes.Contains(reservation.RouteId));
        _layout.Routes.RemoveAll(route => affectedRoutes.Contains(route.Id));
        _layout.BlockOccupancies.RemoveAll(occupancy => selected.Contains(occupancy.BlockId));
        _layout.Blocks.RemoveAll(block => selected.Contains(block.Id));
        _service.Initialize(_layout);
        Refresh();
    }

    private void SetSelectedOccupancy(BlockOccupancyState state)
    {
        if (_layout is null) return;
        var selected = BlocksGrid.SelectedItems.Cast<BlockRow>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Selecciona almenys un bloc.", "Dispatcher", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        foreach (var row in selected) _service.SetOccupancy(_layout, row.Id, state);
        Refresh();
    }

    private void SetUnknown_Click(object sender, RoutedEventArgs e) => SetSelectedOccupancy(BlockOccupancyState.Unknown);
    private void SetFree_Click(object sender, RoutedEventArgs e) => SetSelectedOccupancy(BlockOccupancyState.Free);
    private void SetOccupied_Click(object sender, RoutedEventArgs e) => SetSelectedOccupancy(BlockOccupancyState.Occupied);

    private void CreateRoute_Click(object sender, RoutedEventArgs e)
    {
        if (_layout is null) return;
        try
        {
            var selected = BlocksGrid.SelectedItems.Cast<BlockRow>().Select(x => x.Id).ToList();
            var route = _service.CreateRoute(_layout, RouteNameBox.Text, selected);
            RouteNameBox.Clear();
            Refresh();
            RoutesList.SelectedValue = route.Id;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No s'ha pogut crear l'itinerari", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ReserveRoute_Click(object sender, RoutedEventArgs e)
    {
        if (_layout is null || RoutesList.SelectedItem is not RouteRow selected) return;
        if (_service.TryReserveRoute(_layout, selected.Id, out var reason))
            MessageBox.Show(reason, "Dispatcher", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show(reason, "Reserva rebutjada", MessageBoxButton.OK, MessageBoxImage.Warning);
        Refresh();
    }

    private void ReleaseRoute_Click(object sender, RoutedEventArgs e)
    {
        if (_layout is null || RoutesList.SelectedItem is not RouteRow selected) return;
        if (!_service.ReleaseRoute(_layout, selected.Id))
            MessageBox.Show("Aquest itinerari no tenia cap reserva activa.", "Dispatcher", MessageBoxButton.OK, MessageBoxImage.Information);
        Refresh();
    }

    private void RoutesList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private sealed record BlockRow(Guid Id, string Name, string LevelName, BlockOccupancyState State)
    {
        public string OccupancyLabel => State switch
        {
            BlockOccupancyState.Free => "Lliure",
            BlockOccupancyState.Occupied => "Ocupat",
            _ => "Desconegut"
        };
    }

    private sealed record RouteRow(Guid Id, string Name, string Blocks, bool IsReserved)
    {
        public string DisplayName => $"{Name} · {Blocks}" + (IsReserved ? " · RESERVAT" : "");
    }
}
