using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Platinum.Application.Dispatcher;
using Platinum.Domain.Layout;

namespace Platinum.Presentation.Avalonia.Dispatcher;

public sealed class DispatcherView : UserControl
{
    private readonly DispatcherService _service=new();
    private TrackLayout? _layout;
    private readonly TextBox _blockName=new(){PlaceholderText="Nom del bloc"};
    private readonly ListBox _blocks=new(){MinHeight=170};
    private readonly TextBox _routeName=new(){PlaceholderText="Nom de l'itinerari"};
    private readonly TextBox _routeBlocks=new(){PlaceholderText="Blocs separats per comes"};
    private readonly ListBox _routes=new(){MinHeight=130};
    private readonly TextBlock _status=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Goldenrod};

    public DispatcherView()
    {
        var root=new StackPanel{Margin=new global::Avalonia.Thickness(16),Spacing=8};
        root.Children.Add(new TextBlock{Text="DISPATCHER · BLOCS I ITINERARIS",FontSize=19,FontWeight=FontWeight.Bold});
        root.Children.Add(new TextBlock{Text="Gestió local de reserves. No és un enclavament ferroviari i no envia ordres físiques.",TextWrapping=TextWrapping.Wrap});
        root.Children.Add(_blockName); root.Children.Add(MakeButton("Afegir bloc",AddBlock)); root.Children.Add(_blocks);
        root.Children.Add(MakeButton("Canviar ocupació del bloc",CycleOccupancy));
        root.Children.Add(new Separator()); root.Children.Add(_routeName); root.Children.Add(_routeBlocks);
        root.Children.Add(MakeButton("Crear itinerari",CreateRoute)); root.Children.Add(_routes);
        root.Children.Add(MakeButton("Reservar itinerari",ReserveRoute)); root.Children.Add(MakeButton("Alliberar itinerari",ReleaseRoute));
        root.Children.Add(_status); Content=new ScrollViewer{Content=root};
    }
    public void SetLayout(TrackLayout layout){_layout=layout;_service.Initialize(layout);Refresh();}
    public void SyncToLayout(){if(_layout is not null)_service.Initialize(_layout);}
    private static Button MakeButton(string text,EventHandler<global::Avalonia.Interactivity.RoutedEventArgs> action){var b=new Button{Content=text,HorizontalAlignment=HorizontalAlignment.Left};b.Click+=action;return b;}
    private void AddBlock(object? s,global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if(_layout is null)return; var name=_blockName.Text?.Trim(); if(string.IsNullOrWhiteSpace(name))return;
        if(_layout.Blocks.Any(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase))){_status.Text="Ja existeix aquest bloc.";return;}
        var level=_layout.Levels.FirstOrDefault(); if(level is null){_status.Text="No hi ha cap nivell.";return;}
        _layout.Blocks.Add(new LogicalBlock(Guid.NewGuid(),name,level.Id,Array.Empty<Guid>()));_service.Initialize(_layout);_status.Text="Bloc creat amb ocupació desconeguda.";Refresh();
    }
    private void CycleOccupancy(object? s,global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if(_layout is null||_blocks.SelectedItem is not BlockRow row)return;
        var state=_layout.BlockOccupancies.FirstOrDefault(x=>x.BlockId==row.Id)?.State??BlockOccupancyState.Unknown;
        var next=state switch{BlockOccupancyState.Unknown=>BlockOccupancyState.Free,BlockOccupancyState.Free=>BlockOccupancyState.Occupied,_=>BlockOccupancyState.Unknown};
        _service.SetOccupancy(_layout,row.Id,next);_status.Text="Ocupació lògica actualitzada: "+next+". No és una lectura física.";Refresh();
    }
    private void CreateRoute(object? s,global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if(_layout is null)return;
        try{var names=(_routeBlocks.Text??"").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
            var ids=names.Select(n=>_layout.Blocks.FirstOrDefault(b=>b.Name.Equals(n,StringComparison.OrdinalIgnoreCase))?.Id??throw new InvalidOperationException("No existeix el bloc «"+n+"».")).ToArray();
            var route=_service.CreateRoute(_layout,_routeName.Text??"",ids);_status.Text="Itinerari creat: "+route.Name;Refresh();}
        catch(Exception ex){_status.Text=ex.Message;}
    }
    private void ReserveRoute(object? s,global::Avalonia.Interactivity.RoutedEventArgs e){if(_layout is null||_routes.SelectedItem is not RouteRow row)return;_service.TryReserveRoute(_layout,row.Id,out var reason);_status.Text=reason;Refresh();}
    private void ReleaseRoute(object? s,global::Avalonia.Interactivity.RoutedEventArgs e){if(_layout is null||_routes.SelectedItem is not RouteRow row)return;_service.ReleaseRoute(_layout,row.Id);_status.Text="Reserva alliberada.";Refresh();}
    private void Refresh()
    {
        if(_layout is null)return;var names=_layout.Levels.ToDictionary(x=>x.Id,x=>x.Name);
        _blocks.ItemsSource=_layout.Blocks.Select(b=>new BlockRow(b.Id,b.Name,_layout.BlockOccupancies.FirstOrDefault(o=>o.BlockId==b.Id)?.State??BlockOccupancyState.Unknown,names.GetValueOrDefault(b.LevelId,"?"))).ToList();
        _routes.ItemsSource=_layout.Routes.Select(r=>new RouteRow(r.Id,r.Name,string.Join(" → ",r.BlockIds.Select(id=>_layout.Blocks.FirstOrDefault(b=>b.Id==id)?.Name??"?")),_layout.RouteReservations.Any(x=>x.RouteId==r.Id))).ToList();
    }
    private sealed record BlockRow(Guid Id,string Name,BlockOccupancyState State,string Level){public override string ToString()=>Name+" · "+State+" · "+Level;}
    private sealed record RouteRow(Guid Id,string Name,string Blocks,bool Reserved){public override string ToString()=>Name+" · "+Blocks+" · "+(Reserved?"RESERVAT":"NO RESERVAT");}
}