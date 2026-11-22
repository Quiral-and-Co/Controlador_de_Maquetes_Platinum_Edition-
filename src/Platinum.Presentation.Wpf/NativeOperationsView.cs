using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Platinum.Presentation.Wpf;

/// <summary>
/// Panells d'operació WPF natius. Les funcions són locals i simulades:
/// no obren sockets, no connecten JMRI/CS2, no armen DCC ni comanden maquinari.
/// </summary>
public sealed class NativeOperationsView : UserControl
{
    private readonly ObservableCollection<string> _blocks = [];
    private readonly HashSet<string> _occupied = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<string> _routes = [];
    private readonly ObservableCollection<string> _stations = [];
    private readonly ObservableCollection<string> _events = [];
    private readonly List<CheckBox> _validationChecks = [];
    private readonly TextBlock _dispatcherStatus = Status("Cap bloc creat.");
    private readonly TextBlock _signalStatus = Status("Senyal en estat desconegut.");
    private readonly TextBlock _stationStatus = Status("Cap central connectada. Perfil local només.");
    private readonly TextBlock _dmiStatus = Status("Cabina simulada · sortides físiques bloquejades.");
    private readonly TextBlock _protectionStatus = Status("Protecció virtual preparada.");
    private readonly TextBlock _groundStatus = Status("Terminal local · sense ràdio.");
    private readonly TextBlock _gameStatus = Status("Recursos de simulació locals.");
    private readonly TextBlock _validationStatus = Status("Validació pendent.");

    public NativeOperationsView()
    {
        Background = Brush("#FF18212B");
        var tabs = new TabControl { Background = Brush("#FF18212B"), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
        tabs.Items.Add(Tab("Dispatcher", BuildDispatcher()));
        tabs.Items.Add(Tab("Senyalització", BuildSignalling()));
        tabs.Items.Add(Tab("Centrals", BuildStations()));
        tabs.Items.Add(Tab("DMI locomotora", BuildDmi()));
        tabs.Items.Add(Tab("Protecció tren", BuildProtection()));
        tabs.Items.Add(Tab("Tren-terra", BuildGround()));
        tabs.Items.Add(Tab("Món de Jocs", BuildGame()));
        tabs.Items.Add(Tab("Validació", BuildValidation()));
        var root = new DockPanel();
        var notice = Notice();
        DockPanel.SetDock(notice, Dock.Top);
        root.Children.Add(notice);
        root.Children.Add(tabs);
        Content = root;
    }

    private static UIElement Notice() => new Border
    {
        Background = Brush("#FF3A2D1B"), Padding = new Thickness(12), Margin = new Thickness(8, 8, 8, 4),
        Child = new TextBlock
        {
            Text = "MODE NATIU WPF · SIMULACIÓ I PLANIFICACIÓ LOCAL. No hi ha connexió amb JMRI, CS2/CS3 ni maquinari. DCC i totes les sortides físiques romanen bloquejades.",
            TextWrapping = TextWrapping.Wrap, Foreground = Brush("#FFFFD58A"), FontWeight = FontWeights.SemiBold
        }
    };

    private UIElement BuildDispatcher()
    {
        var root = TwoColumns();
        var left = Panel("Blocs lògics");
        var name = new TextBox { Text = "Bloc nou", MinWidth = 180, Margin = new Thickness(0, 4, 0, 8) };
        var list = new ListBox { MinHeight = 180, Background = Brush("#FF111820"), Foreground = Brushes.White };
        void Refresh() { var selected = list.SelectedIndex; list.ItemsSource = _blocks.Select(b => (_occupied.Contains(b) ? "● OCUPAT · " : "○ LLIURE · ") + b).ToList(); if (selected >= 0 && selected < list.Items.Count) list.SelectedIndex = selected; }
        left.Children.Add(Label("Nom del bloc")); left.Children.Add(name);
        left.Children.Add(Button("Afegir bloc", (_, _) =>
        {
            var value = name.Text.Trim();
            if (value.Length == 0 || _blocks.Contains(value)) return;
            _blocks.Add(value); Refresh(); _dispatcherStatus.Text = $"Bloc «{value}» creat localment.";
        }));
        left.Children.Add(list);
        left.Children.Add(Button("Canviar lliure / ocupat", (_, _) =>
        {
            if (list.SelectedItem is not string selected) return;
            var value = selected[(selected.IndexOf('·') + 1)..].Trim();
            if (_occupied.Contains(value)) _occupied.Remove(value); else _occupied.Add(value);
            Refresh(); _dispatcherStatus.Text = $"Estat local actualitzat: {value}.";
        }));
        left.Children.Add(_dispatcherStatus);

        var right = Panel("Itineraris locals");
        var routeName = new TextBox { Text = "Itinerari 1", Margin = new Thickness(0, 4, 0, 8) };
        var routeBlocks = new TextBox { Text = "", AcceptsReturn = true, MinHeight = 70, ToolTip = "Noms dels blocs separats per comes." };
        var routeList = new ListBox { ItemsSource = _routes, MinHeight = 150, Background = Brush("#FF111820"), Foreground = Brushes.White };
        right.Children.Add(Label("Nom")); right.Children.Add(routeName); right.Children.Add(Label("Blocs (separats per comes)")); right.Children.Add(routeBlocks);
        right.Children.Add(Button("Crear itinerari", (_, _) =>
        {
            var blocks = routeBlocks.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (blocks.Length == 0 || blocks.Any(b => !_blocks.Contains(b))) { _dispatcherStatus.Text = "Itinerari rebutjat: indica blocs existents."; return; }
            _routes.Add($"{routeName.Text.Trim()} · {string.Join(" → ", blocks)} · NO RESERVAT");
        }));
        right.Children.Add(routeList);
        right.Children.Add(Button("Comprovar disponibilitat local", (_, _) =>
        {
            if (routeList.SelectedItem is not string route) return;
            var parts = route.Split('·');
            var names = parts.Length > 1 ? parts[1].Split('→', StringSplitOptions.TrimEntries) : Array.Empty<string>();
            var conflicts = names.Where(_occupied.Contains).ToArray();
            _dispatcherStatus.Text = conflicts.Length == 0 ? "Simulació: blocs disponibles. No és un enclavament." : "Blocs ocupats: " + string.Join(", ", conflicts);
        }));
        AddColumn(root, left); AddColumn(root, right);
        return root;
    }

    private UIElement BuildSignalling()
    {
        var panel = Panel("Senyalització ferroviària · simulador local");
        var signal = new ComboBox { ItemsSource = new[] { "S1 · Entrada", "S2 · Sortida", "S3 · Bloqueig", "S4 · Maniobres" }, SelectedIndex = 0, Margin = new Thickness(0, 6, 0, 10) };
        var aspect = new ComboBox { ItemsSource = new[] { "Desconegut", "Via lliure (simulat)", "Precaució (simulat)", "Parada (simulat)", "Maniobres (simulat)" }, SelectedIndex = 0, Margin = new Thickness(0, 6, 0, 10) };
        panel.Children.Add(Label("Senyal")); panel.Children.Add(signal); panel.Children.Add(Label("Aspecte")); panel.Children.Add(aspect);
        panel.Children.Add(Button("Aplicar aspecte al simulador", (_, _) => _signalStatus.Text = $"{signal.SelectedItem}: {aspect.SelectedItem}. Estat fictici, no enviat a cap senyal real."));
        panel.Children.Add(_signalStatus);
        panel.Children.Add(Label("La representació ADIF/JMRI i l'enclavament real requereixen dades de camp, protocols i validació independents."));
        return Scroll(panel);
    }

    private UIElement BuildStations()
    {
        var panel = Panel("Perfils de centrals digitals");
        var manufacturer = new ComboBox { ItemsSource = new[] { "Märklin", "Roco", "ESU", "Uhlenbrock", "Z21", "DCC genèric" }, SelectedIndex = 0, Margin = new Thickness(0, 6, 0, 8) };
        var model = new TextBox { Text = "CS2 / CS3 / model", Margin = new Thickness(0, 4, 0, 8) };
        var host = new TextBox { Text = "192.168.1.50", Margin = new Thickness(0, 4, 0, 8) };
        var port = new TextBox { Text = "15731", Margin = new Thickness(0, 4, 0, 8) };
        var profiles = new ListBox { ItemsSource = _stations, MinHeight = 130, Background = Brush("#FF111820"), Foreground = Brushes.White };
        panel.Children.Add(Label("Fabricant")); panel.Children.Add(manufacturer); panel.Children.Add(Label("Model")); panel.Children.Add(model);
        panel.Children.Add(Label("Host / IP")); panel.Children.Add(host); panel.Children.Add(Label("Port")); panel.Children.Add(port);
        panel.Children.Add(Button("Desar perfil local", (_, _) =>
        {
            if (!int.TryParse(port.Text, out var p) || p is < 1 or > 65535) { _stationStatus.Text = "Port invàlid."; return; }
            _stations.Add($"{manufacturer.SelectedItem} · {model.Text.Trim()} · {host.Text.Trim()}:{p} · NO CONNECTAT");
            _stationStatus.Text = "Perfil desat en memòria d'aquesta sessió. No s'ha obert cap connexió.";
        }));
        panel.Children.Add(profiles); panel.Children.Add(_stationStatus);
        panel.Children.Add(Label("La connexió de xarxa/sèrie no s'habilita en aquesta fase. Cap perfil no arma DCC."));
        return Scroll(panel);
    }

    private UIElement BuildDmi()
    {
        var panel = Panel("DMI · cabina virtual de locomotora");
        var speed = new Slider { Minimum = 0, Maximum = 200, Value = 0, TickFrequency = 10, Margin = new Thickness(0, 10, 0, 10) };
        var readout = Label("Velocitat simulada: 0 km/h");
        speed.ValueChanged += (_, _) => { readout.Text = $"Velocitat simulada: {speed.Value:0} km/h"; _dmiStatus.Text = speed.Value > 0 ? "Cabina virtual activa. Cap ordre enviada." : "Locomotora virtual aturada."; };
        panel.Children.Add(readout); panel.Children.Add(speed);
        panel.Children.Add(Button("Parada virtual", (_, _) => { speed.Value = 0; _dmiStatus.Text = "Parada virtual aplicada. No és una parada d'emergència física."; }));
        panel.Children.Add(_dmiStatus);
        panel.Children.Add(Label("Les funcions F0–F28, direcció i regulador físic necessiten un adaptador de protocol verificat abans d'ús real."));
        return Scroll(panel);
    }

    private UIElement BuildProtection()
    {
        var panel = Panel("Protecció ferroviària · simulador educatiu");
        var permitted = new Slider { Minimum = 0, Maximum = 160, Value = 80, TickFrequency = 10, Margin = new Thickness(0, 8, 0, 8) };
        var actual = new Slider { Minimum = 0, Maximum = 200, Value = 0, TickFrequency = 10, Margin = new Thickness(0, 8, 0, 8) };
        var readout = Label("Velocitat 0 · límit 80 km/h");
        void Evaluate()
        {
            readout.Text = $"Velocitat {actual.Value:0} · límit {permitted.Value:0} km/h";
            _protectionStatus.Text = actual.Value > permitted.Value ? "FRENADA VIRTUAL: s'ha superat el límit." : "Supervisió virtual dins del límit.";
        }
        permitted.ValueChanged += (_, _) => Evaluate(); actual.ValueChanged += (_, _) => Evaluate();
        panel.Children.Add(Label("Velocitat permesa")); panel.Children.Add(permitted);
        panel.Children.Add(Label("Velocitat del tren simulat")); panel.Children.Add(actual);
        panel.Children.Add(readout); panel.Children.Add(Button("Reconèixer avís", (_, _) => _protectionStatus.Text = "Avís reconegut en el simulador."));
        panel.Children.Add(Button("Emergència virtual", (_, _) => { actual.Value = 0; _protectionStatus.Text = "Emergència virtual: velocitat simulada a zero."; }));
        panel.Children.Add(_protectionStatus);
        panel.Children.Add(Label("No és ETCS/ASFA/LZB/PZB certificat ni controla frens reals."));
        return Scroll(panel);
    }

    private UIElement BuildGround()
    {
        var panel = Panel("Terminal tren-terra · missatgeria local");
        var destination = new TextBox { Text = "Tren 001", Margin = new Thickness(0, 6, 0, 8) };
        var message = new TextBox { AcceptsReturn = true, MinHeight = 70, Text = "Missatge de prova", Margin = new Thickness(0, 4, 0, 8) };
        var log = new ListBox { ItemsSource = _events, MinHeight = 150, Background = Brush("#FF111820"), Foreground = Brushes.White };
        panel.Children.Add(Label("Destinatari")); panel.Children.Add(destination); panel.Children.Add(Label("Missatge")); panel.Children.Add(message);
        panel.Children.Add(Button("Registrar missatge local", (_, _) =>
        {
            if (message.Text.Trim().Length == 0) return;
            _events.Add($"{DateTime.Now:HH:mm:ss} · {destination.Text.Trim()}: {message.Text.Trim()}");
            _groundStatus.Text = "Missatge registrat localment. No s'ha transmès per ràdio.";
        }));
        panel.Children.Add(log); panel.Children.Add(_groundStatus);
        return Scroll(panel);
    }

    private UIElement BuildGame()
    {
        var panel = Panel("Märklin · Món de Jocs (simulació)");
        var fuel = new Slider { Minimum = 0, Maximum = 100, Value = 65, TickFrequency = 5, Margin = new Thickness(0, 8, 0, 8) };
        var readout = Label("Combustible virtual: 65%");
        fuel.ValueChanged += (_, _) => readout.Text = $"Combustible virtual: {fuel.Value:0}%";
        panel.Children.Add(readout); panel.Children.Add(fuel);
        panel.Children.Add(Button("Reomplir dipòsit virtual", (_, _) => fuel.Value = 100));
        panel.Children.Add(_gameStatus);
        panel.Children.Add(Label("No llegeix ni escriu recursos mfx+ i no envia ordres a la CS2."));
        return Scroll(panel);
    }

    private UIElement BuildValidation()
    {
        var panel = Panel("Matriu de verificació · registres d'operador");
        panel.Children.Add(Label("Marca només les comprovacions que hagis inspeccionat. Aquest registre no certifica el circuit ni desbloqueja cap sortida."));
        foreach (var item in new[] { "Traçat mecànic inspeccionat", "Connexions elèctriques inspeccionades", "Sensors identificats i provats", "Desviaments verificats físicament", "Parada d'emergència física provada", "Alimentació i proteccions inspeccionades" })
        {
            var check = new CheckBox { Content = item, Margin = new Thickness(4, 7, 4, 7), Foreground = Brushes.White };
            _validationChecks.Add(check); panel.Children.Add(check);
        }
        panel.Children.Add(Button("Calcular estat de revisió", (_, _) =>
        {
            var done = _validationChecks.Count(x => x.IsChecked == true);
            _validationStatus.Text = $"Revisió declarada: {done}/{_validationChecks.Count}. SORTIDES FÍSIQUES: BLOQUEJADES.";
        }));
        panel.Children.Add(_validationStatus);
        return Scroll(panel);
    }

    private static TabItem Tab(string title, UIElement content) => new() { Header = title, Content = content, Padding = new Thickness(10) };
    private static StackPanel Panel(string title)
    {
        var panel = new StackPanel { Margin = new Thickness(18), MaxWidth = 900 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brush("#FFFFC65C"), Margin = new Thickness(0, 0, 0, 14) });
        return panel;
    }
    private static Grid TwoColumns()
    {
        var grid = new Grid { Margin = new Thickness(12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition());
        return grid;
    }
    private static void AddColumn(Grid grid, UIElement child)
    {
        var index = grid.Children.Count;
        var border = new Border { Child = child, Background = Brush("#FF202833"), BorderBrush = Brush("#FF394654"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(5) };
        Grid.SetColumn(border, index); grid.Children.Add(border);
    }
    private static TextBlock Label(string text) => new() { Text = text, Foreground = Brush("#FFE9EDF2"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 2) };
    private static TextBlock Status(string text) => new() { Text = text, Foreground = Brush("#FFFFD58A"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 4) };
    private static Button Button(string text, RoutedEventHandler action)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 170 };
        button.Click += action; return button;
    }
    private static ScrollViewer Scroll(UIElement content) => new() { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
}