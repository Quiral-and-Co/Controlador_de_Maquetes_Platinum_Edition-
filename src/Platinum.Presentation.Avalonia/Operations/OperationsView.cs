using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Platinum.Application.Control;
using Platinum.Domain.Control;
using Platinum.Infrastructure.Control;

namespace Platinum.Presentation.Avalonia.Operations;

public sealed class OperationsView : UserControl
{
    private readonly RailwayControlService _controlService=new(new LocalSimulationAdapter());
    private readonly TextBlock _status=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Goldenrod};
    private readonly List<string> _blocks=[],_routes=[],_events=[];
    private readonly ListBox _blockList=new(){MinHeight=90},_routeList=new(){MinHeight=90},_eventList=new(){MinHeight=100};
    public OperationsView()
    {
        var tabs=new TabControl();
        tabs.Items.Add(new TabItem{Header="Control propi",Content=BuildNativeControl()});
        tabs.Items.Add(new TabItem{Header="Automatització",Content=BuildAutomation()});
        tabs.Items.Add(new TabItem{Header="Senyalització",Content=BuildSignalling()});
        tabs.Items.Add(new TabItem{Header="Centrals",Content=BuildStations()});
        tabs.Items.Add(new TabItem{Header="Cabina DMI",Content=BuildDmi()});
        tabs.Items.Add(new TabItem{Header="Protecció",Content=BuildProtection()});
        tabs.Items.Add(new TabItem{Header="Tren-terra",Content=BuildGround()});
        tabs.Items.Add(new TabItem{Header="Món de Jocs",Content=BuildGame()});
        tabs.Items.Add(new TabItem{Header="Validació",Content=BuildValidation()});
        Content=tabs;
    }
    private static Button Btn(string text,EventHandler<global::Avalonia.Interactivity.RoutedEventArgs> action){var b=new Button{Content=text,HorizontalAlignment=global::Avalonia.Layout.HorizontalAlignment.Left};b.Click+=action;return b;}
    private static TextBlock Label(string text)=>new(){Text=text,TextWrapping=TextWrapping.Wrap,Margin=new global::Avalonia.Thickness(0,5,0,2)};
    private static StackPanel Panel(string title){var p=new StackPanel{Margin=new global::Avalonia.Thickness(16),Spacing=8};p.Children.Add(new TextBlock{Text=title,FontSize=18,FontWeight=FontWeight.Bold});return p;}
    private static ScrollViewer Scroll(Control c)=>new(){Content=c};
    private Control BuildNativeControl()
    {
        var p=Panel("Motor de control propi · central virtual Platinum");
        var status=Label("Central virtual desconnectada. No hi ha connexió física.");
        var address=new TextBox{Text="3",Width=150};
        var speed=new Slider{Minimum=0,Maximum=1000,Value=0};
        var reverse=new CheckBox{Content="Marxa enrere"};
        var function=new ComboBox{ItemsSource=Enumerable.Range(0,69).Select(x=>"F"+x).ToList(),SelectedIndex=0,Width=150};
        var functionOn=new CheckBox{Content="Activar funció"};
        var accessoryAddress=new TextBox{Text="1",Width=150};
        var accessoryState=new ComboBox{ItemsSource=Enum.GetValues<AccessoryState>(),SelectedIndex=0,Width=180};
        p.Children.Add(status);
        p.Children.Add(Btn("Connectar central virtual",async (_,_)=>{try{await _controlService.ConnectAsync();status.Text="CONNECTAT · "+_controlService.Profile.Name+" · només simulació local.";}catch(Exception ex){status.Text="Error: "+ex.Message;}}));
        p.Children.Add(Btn("Desconnectar",async (_,_)=>{await _controlService.DisconnectAsync();status.Text="Central virtual desconnectada.";}));
        p.Children.Add(Label("Adreça de locomotora (1–10239)"));p.Children.Add(address);
        p.Children.Add(Label("Velocitat virtual (0–1000)"));p.Children.Add(speed);p.Children.Add(reverse);
        p.Children.Add(Btn("Aplicar velocitat a la simulació",async (_,_)=>{if(!int.TryParse(address.Text,out var a)){status.Text="Adreça de locomotora no vàlida.";return;}var r=await _controlService.SetLocomotiveSpeedAsync(a,(int)speed.Value,reverse.IsChecked==true);status.Text=r.Message;}));
        p.Children.Add(Label("Funció de locomotora"));p.Children.Add(function);p.Children.Add(functionOn);
        p.Children.Add(Btn("Aplicar funció a la simulació",async (_,_)=>{if(!int.TryParse(address.Text,out var a)){status.Text="Adreça no vàlida.";return;}var r=await _controlService.SetFunctionAsync(a,function.SelectedIndex,functionOn.IsChecked==true);status.Text=r.Message;}));
        p.Children.Add(Label("Adreça d'accessori / desviament"));p.Children.Add(accessoryAddress);p.Children.Add(accessoryState);
        p.Children.Add(Btn("Aplicar accessori a la simulació",async (_,_)=>{if(!int.TryParse(accessoryAddress.Text,out var a)||accessoryState.SelectedItem is not AccessoryState state){status.Text="Adreça o estat d'accessori no vàlid.";return;}var r=await _controlService.SetAccessoryAsync(a,state);status.Text=r.Message;}));
        p.Children.Add(Btn("ATURADA D'EMERGÈNCIA VIRTUAL",async (_,_)=>{var r=await _controlService.EmergencyStopAsync();speed.Value=0;status.Text=r.Message;}));
        p.Children.Add(Label("Aquesta vista utilitza RailwayControlService i LocalSimulationAdapter. No s'obre cap port ni s'envien ordres a locomotores, desviaments o senyals reals."));
        return Scroll(p);
    }

    private Control BuildAutomation()
    {
        var p=Panel("Dispatcher auxiliar i automatització local");var name=new TextBox{PlaceholderText="Nom del bloc"};var routeName=new TextBox{PlaceholderText="Nom de l'itinerari"};var routeBlocks=new TextBox{PlaceholderText="Blocs separats per comes"};
        p.Children.Add(Label("Blocs lògics de simulació"));p.Children.Add(name);
        p.Children.Add(Btn("Afegir bloc",(_,_)=>{var n=name.Text?.Trim();if(string.IsNullOrWhiteSpace(n)||_blocks.Contains(n))return;_blocks.Add(n+" · LLIURE");RefreshLists();_status.Text="Bloc local creat: "+n;}));
        p.Children.Add(_blockList);
        p.Children.Add(Btn("Canviar lliure / ocupat",(_,_)=>{if(_blockList.SelectedItem is not string selected)return;var i=_blocks.IndexOf(selected);_blocks[i]=selected.EndsWith("OCUPAT")?selected.Replace("OCUPAT","LLIURE"):selected.Replace("LLIURE","OCUPAT");RefreshLists();_status.Text="Estat simulat canviat; no és una lectura de sensor.";}));
        p.Children.Add(new Separator());p.Children.Add(routeName);p.Children.Add(routeBlocks);
        p.Children.Add(Btn("Crear itinerari local",(_,_)=>{var values=(routeBlocks.Text??"").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);if(values.Length==0||values.Any(v=>!_blocks.Any(b=>b.StartsWith(v+" · ",StringComparison.OrdinalIgnoreCase)))){_status.Text="Indica blocs existents.";return;}_routes.Add((routeName.Text??"Itinerari").Trim()+" · "+string.Join(" → ",values)+" · NO RESERVAT");RefreshLists();}));
        p.Children.Add(_routeList);
        p.Children.Add(Btn("Reservar itinerari (simulació)",(_,_)=>{if(_routeList.SelectedItem is not string route)return;var parts=route.Split(" · ");var names=parts.Length>1?parts[1].Split(" → ",StringSplitOptions.TrimEntries):Array.Empty<string>();if(names.Any(n=>_blocks.Any(b=>b.StartsWith(n+" · OCUPAT",StringComparison.OrdinalIgnoreCase)))){_status.Text="Reserva rebutjada: hi ha blocs ocupats.";return;}var i=_routes.IndexOf(route);_routes[i]=route.Replace("NO RESERVAT","RESERVAT");RefreshLists();_status.Text="Reserva lògica local creada; cap sortida física activada.";}));
        p.Children.Add(Btn("Alliberar reserva",(_,_)=>{if(_routeList.SelectedItem is not string route)return;var i=_routes.IndexOf(route);_routes[i]=route.Replace("RESERVAT","NO RESERVAT");RefreshLists();}));
        p.Children.Add(Btn("ATURADA D'EMERGÈNCIA LÒGICA",(_,_)=>{_status.Text="Aturada lògica enclavada en aquest simulador. No actua sobre maquinari.";_routes.Clear();RefreshLists();}));
        p.Children.Add(_status);p.Children.Add(Label("Aquest simulador no és un enclavament ferroviari certificat. La parada física independent continua sent obligatòria."));return Scroll(p);
    }
    private void RefreshLists(){_blockList.ItemsSource=null;_blockList.ItemsSource=_blocks.ToList();_routeList.ItemsSource=null;_routeList.ItemsSource=_routes.ToList();}
    private Control BuildSignalling()
    {
        var p=Panel("Senyalització ferroviària · simulador local");var signal=new ComboBox{ItemsSource=new[]{"S1 · Entrada","S2 · Sortida","S3 · Bloqueig","S4 · Maniobres"},SelectedIndex=0};var aspect=new ComboBox{ItemsSource=new[]{"Desconegut","Via lliure (simulat)","Precaució (simulat)","Parada (simulat)","Maniobres (simulat)"},SelectedIndex=0};
        p.Children.Add(Label("Senyal"));p.Children.Add(signal);p.Children.Add(Label("Aspecte"));p.Children.Add(aspect);p.Children.Add(Btn("Aplicar aspecte al simulador",(_,_)=>_status.Text=signal.SelectedItem+": "+aspect.SelectedItem+". No s'ha enviat a cap senyal real."));p.Children.Add(_status);p.Children.Add(Label("La integració ADIF/JMRI i l'enclavament real necessiten dades de camp, protocols i validació independent."));return Scroll(p);
    }
    private Control BuildStations()
    {
        var p=Panel("Perfils de centrals digitals");var maker=new ComboBox{ItemsSource=new[]{"Märklin","Roco","ESU","Uhlenbrock","Z21","DCC genèric"},SelectedIndex=0};var model=new TextBox{PlaceholderText="Model de central"};var host=new TextBox{PlaceholderText="Adreça IP"};var port=new TextBox{Text="15731"};var profiles=new ListBox{MinHeight=90};var values=new List<string>();
        p.Children.Add(Label("Fabricant"));p.Children.Add(maker);p.Children.Add(model);p.Children.Add(host);p.Children.Add(port);
        p.Children.Add(Btn("Desar perfil local",(_,_)=>{if(!int.TryParse(port.Text,out var n)||n<1||n>65535){_status.Text="Port invàlid.";return;}values.Add(maker.SelectedItem+" · "+model.Text+" · "+host.Text+":"+n+" · NO CONNECTAT");profiles.ItemsSource=values.ToList();_status.Text="Perfil desat localment. No s'ha obert cap connexió.";}));
        p.Children.Add(profiles);p.Children.Add(Label("Les proves de central continuen desactivades fins a portar i validar els adaptadors de protocol."));return Scroll(p);
    }
    private Control BuildDmi()
    {
        var p=Panel("DMI · cabina virtual de locomotora");var speed=new Slider{Minimum=0,Maximum=200,Value=0};var readout=Label("Velocitat simulada: 0 km/h");
        speed.PropertyChanged+=(s,e)=>{if(e.Property==RangeBase.ValueProperty)readout.Text="Velocitat simulada: "+speed.Value.ToString("0")+" km/h";};
        p.Children.Add(readout);p.Children.Add(speed);p.Children.Add(Btn("Parada virtual",(_,_)=>{speed.Value=0;_status.Text="Parada virtual aplicada. No és una parada física.";}));p.Children.Add(_status);return Scroll(p);
    }
    private Control BuildProtection()
    {
        var p=Panel("Protecció ferroviària · simulador educatiu");var limit=new Slider{Minimum=0,Maximum=160,Value=80};var actual=new Slider{Minimum=0,Maximum=200,Value=0};var readout=Label("Velocitat 0 · límit 80 km/h");
        void Update(){readout.Text="Velocitat "+actual.Value.ToString("0")+" · límit "+limit.Value.ToString("0")+" km/h";_status.Text=actual.Value>limit.Value?"FRENADA VIRTUAL: límit superat.":"Supervisió virtual dins del límit.";}
        limit.PropertyChanged+=(s,e)=>{if(e.Property==RangeBase.ValueProperty)Update();};actual.PropertyChanged+=(s,e)=>{if(e.Property==RangeBase.ValueProperty)Update();};
        p.Children.Add(Label("Velocitat permesa"));p.Children.Add(limit);p.Children.Add(Label("Velocitat simulada"));p.Children.Add(actual);p.Children.Add(readout);p.Children.Add(Btn("Reconèixer avís",(_,_)=>_status.Text="Avís reconegut en el simulador."));p.Children.Add(Btn("Emergència virtual",(_,_)=>{actual.Value=0;_status.Text="Emergència virtual: velocitat simulada a zero.";}));p.Children.Add(_status);p.Children.Add(Label("No és ETCS/ASFA/LZB/PZB certificat ni controla frens reals."));return Scroll(p);
    }
    private Control BuildGround()
    {
        var p=Panel("Terminal tren-terra · missatgeria local");var destination=new TextBox{Text="Tren 001"};var message=new TextBox{Text="Missatge de prova",AcceptsReturn=true,MinHeight=70};
        p.Children.Add(Label("Destinatari"));p.Children.Add(destination);p.Children.Add(Label("Missatge"));p.Children.Add(message);
        p.Children.Add(Btn("Registrar missatge local",(_,_)=>{if(string.IsNullOrWhiteSpace(message.Text))return;_events.Add(DateTime.Now.ToString("HH:mm:ss")+" · "+destination.Text+": "+message.Text);_eventList.ItemsSource=_events.ToList();_status.Text="Missatge registrat localment; no s'ha transmès per ràdio.";}));p.Children.Add(_eventList);p.Children.Add(_status);return Scroll(p);
    }
    private Control BuildGame()
    {
        var p=Panel("Märklin · Món de Jocs (simulació)");var fuel=new Slider{Minimum=0,Maximum=100,Value=65};var readout=Label("Combustible virtual: 65%");
        fuel.PropertyChanged+=(s,e)=>{if(e.Property==RangeBase.ValueProperty)readout.Text="Combustible virtual: "+fuel.Value.ToString("0")+"%";};
        p.Children.Add(readout);p.Children.Add(fuel);p.Children.Add(Btn("Reomplir dipòsit virtual",(_,_)=>fuel.Value=100));p.Children.Add(Label("No llegeix ni escriu recursos mfx+ i no envia ordres a la CS2."));return Scroll(p);
    }
    private Control BuildValidation()
    {
        var p=Panel("Matriu de verificació · registres d'operador");p.Children.Add(Label("Aquest registre no certifica el circuit ni desbloqueja sortides."));
        var checks=new[]{"Traçat mecànic inspeccionat","Connexions elèctriques inspeccionades","Sensors identificats i provats","Desviaments verificats físicament","Parada d'emergència física provada","Alimentació i proteccions inspeccionades"}.Select(x=>new CheckBox{Content=x,Margin=new global::Avalonia.Thickness(4,6)}).ToList();
        foreach(var c in checks)p.Children.Add(c);
        p.Children.Add(Btn("Calcular estat de revisió",(_,_)=>_status.Text="Revisió declarada: "+checks.Count(x=>x.IsChecked==true)+"/"+checks.Count+". SORTIDES FÍSIQUES: BLOQUEJADES."));p.Children.Add(_status);return Scroll(p);
    }
}