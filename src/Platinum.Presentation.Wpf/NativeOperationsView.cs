using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Platinum.Application.Control;
using Platinum.Domain.Control;
using Platinum.Infrastructure.Control;
using Platinum.Domain.Layout;

namespace Platinum.Presentation.Wpf;

/// <summary>
/// Panells d'operació WPF natius. El comandament és virtual; la pestanya de centrals
/// només fa una consulta de presència CS2. No arma DCC ni envia ordres físiques.
/// </summary>
public sealed class NativeOperationsView : UserControl
{
    private TrackLayout? _layout;
    private ListBox? _dispatcherBlocksList;
    private ListBox? _dispatcherRoutesList;
    public event EventHandler? ProjectDataChanged;

    private readonly RailwayControlService _controlService = new(new LocalSimulationAdapter());
    private readonly RailwayAutomationEngine _automation = new();
    private MarklinCs2Adapter? _cs2Adapter;
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
        tabs.Items.Add(Tab("Control propi", BuildNativeControl()));
        tabs.Items.Add(Tab("Enclavaments propis", BuildAutomation()));
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
            Text = "MOTOR PROPI PLATINUM · WPF NATIU. No depèn de JMRI. Inclou prova de presència CS2 per protocol natiu; les ordres físiques continuen bloquejades.",
            TextWrapping = TextWrapping.Wrap, Foreground = Brush("#FFFFD58A"), FontWeight = FontWeights.SemiBold
        }
    };

    private UIElement BuildNativeControl()
    {
        var panel = Panel("Motor de control propi · central virtual");
        var connection = Status("Central virtual desconnectada.");
        var address = new TextBox { Text = "3", Margin = new Thickness(0, 4, 0, 8), Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        var speed = new Slider { Minimum = 0, Maximum = 1000, Value = 0, TickFrequency = 50, Margin = new Thickness(0, 8, 0, 8) };
        var reverse = new CheckBox { Content = "Marxa enrere", Foreground = Brushes.White, Margin = new Thickness(0, 5, 0, 8) };
        var function = new ComboBox { ItemsSource = Enumerable.Range(0, 29).Select(x => $"F{x}"), SelectedIndex = 0, Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        var functionOn = new CheckBox { Content = "Activar funció", Foreground = Brushes.White, Margin = new Thickness(0, 5, 0, 8) };
        var accessoryAddress = new TextBox { Text = "1", Width = 120, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 8) };
        var accessoryState = new ComboBox
        {
            ItemsSource = Enum.GetValues<AccessoryState>(),
            SelectedIndex = 0,
            Width = 180,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 8)
        };

        panel.Children.Add(connection);
        panel.Children.Add(Button("Connectar central virtual", async (_, _) =>
        {
            try
            {
                await _controlService.ConnectAsync();
                connection.Text = "CONNECTAT · Platinum Simulation Station · cap connexió física.";
            }
            catch (Exception ex) { connection.Text = "Error de connexió virtual: " + ex.Message; }
        }));
        panel.Children.Add(Button("Desconnectar", async (_, _) =>
        {
            await _controlService.DisconnectAsync();
            connection.Text = "Central virtual desconnectada.";
        }));

        panel.Children.Add(Label("Adreça de locomotora"));
        panel.Children.Add(address);
        panel.Children.Add(Label("Velocitat (passos interns 0–1000)"));
        panel.Children.Add(speed);
        panel.Children.Add(reverse);
        panel.Children.Add(Button("Enviar velocitat a simulació", async (_, _) =>
        {
            if (!int.TryParse(address.Text, out var locoAddress))
            {
                connection.Text = "Adreça de locomotora no vàlida.";
                return;
            }
            var result = await _controlService.SetLocomotiveSpeedAsync(locoAddress, (int)speed.Value, reverse.IsChecked == true);
            connection.Text = result.Message;
        }));

        panel.Children.Add(Label("Funcions de locomotora"));
        panel.Children.Add(function);
        panel.Children.Add(functionOn);
        panel.Children.Add(Button("Enviar funció a simulació", async (_, _) =>
        {
            if (!int.TryParse(address.Text, out var locoAddress))
            {
                connection.Text = "Adreça de locomotora no vàlida.";
                return;
            }
            var functionNumber = function.SelectedIndex;
            var result = await _controlService.SetFunctionAsync(locoAddress, functionNumber, functionOn.IsChecked == true);
            connection.Text = result.Message;
        }));

        panel.Children.Add(Label("Accessori / desviament"));
        panel.Children.Add(accessoryAddress);
        panel.Children.Add(accessoryState);
        panel.Children.Add(Button("Enviar accessori a simulació", async (_, _) =>
        {
            if (!int.TryParse(accessoryAddress.Text, out var accessory))
            {
                connection.Text = "Adreça d'accessori no vàlida.";
                return;
            }
            var result = await _controlService.SetAccessoryAsync(accessory, (AccessoryState)accessoryState.SelectedItem);
            connection.Text = result.Message;
        }));
        panel.Children.Add(Button("ATURADA D'EMERGÈNCIA VIRTUAL", async (_, _) =>
        {
            var result = await _controlService.EmergencyStopAsync();
            speed.Value = 0;
            connection.Text = result.Message;
        }));
        panel.Children.Add(Label("Aquest panell ja utilitza els contractes i el servei de control propis de Platinum. La central virtual és local; no envia ordres a vies, locomotores ni accessoris reals."));
        return Scroll(panel);
    }


    private UIElement BuildAutomation()
    {
        var panel = Panel("Enclavaments i automatització · motor Platinum");
        var blockName = new TextBox { Text = "B1", Width = 180, Margin = new Thickness(0, 4, 0, 8) };
        var blockList = new ListBox { MinHeight = 110, Background = Brush("#FF111820"), Foreground = Brushes.White };
        var routeName = new TextBox { Text = "R1", Width = 180, Margin = new Thickness(0, 4, 0, 8) };
        var routeBlocks = new TextBox { Text = "B1", Width = 280, Margin = new Thickness(0, 4, 0, 8) };
        var routeList = new ListBox { MinHeight = 90, Background = Brush("#FF111820"), Foreground = Brushes.White };
        var signalName = new TextBox { Text = "S1", Width = 180, Margin = new Thickness(0, 4, 0, 8) };
        var protectedBlock = new TextBox { Text = "B1", Width = 180, Margin = new Thickness(0, 4, 0, 8) };
        var status = Status("Motor propi preparat. Sortides físiques bloquejades.");
        var audit = new ListBox { MinHeight = 110, Background = Brush("#FF111820"), Foreground = Brushes.White };

        void Refresh()
        {
            blockList.ItemsSource = _automation.Blocks.Select(b =>
                $"{b.Name} · {(b.Occupied ? "OCUPAT" : "LLIURE")} · {(b.ReservedBy ?? "SENSE RESERVA")}").ToArray();
            routeList.ItemsSource = _automation.Routes.Select(r =>
                $"{r.Name} · {string.Join(" → ", r.Blocks)} · {(r.Active ? "ACTIU" : "INACTIU")}").ToArray();
            audit.ItemsSource = _automation.Events.Reverse().Select(e =>
                $"{e.Timestamp.LocalDateTime:HH:mm:ss} · {e.Code} · {e.Message}").ToArray();
        }

        panel.Children.Add(Label("Crear bloc"));
        panel.Children.Add(blockName);
        panel.Children.Add(Button("Afegir bloc", (_, _) =>
        {
            try { _automation.AddBlock(blockName.Text); status.Text = "Bloc creat."; Refresh(); }
            catch (Exception ex) { status.Text = ex.Message; }
        }));
        panel.Children.Add(blockList);
        panel.Children.Add(Button("Canviar ocupació del bloc seleccionat", (_, _) =>
        {
            if (blockList.SelectedItem is not string selected) return;
            var name = selected.Split('·')[0].Trim();
            var block = _automation.Blocks.First(x => x.Name == name);
            try { _automation.SetOccupancy(name, !block.Occupied); status.Text = "Ocupació actualitzada."; Refresh(); }
            catch (Exception ex) { status.Text = ex.Message; }
        }));

        panel.Children.Add(Label("Itinerari · blocs separats per comes"));
        panel.Children.Add(routeName);
        panel.Children.Add(routeBlocks);
        panel.Children.Add(Button("Reservar itinerari", (_, _) =>
        {
            try
            {
                _automation.ReserveRoute(routeName.Text,
                    routeBlocks.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                status.Text = "Itinerari reservat."; Refresh();
            }
            catch (Exception ex) { status.Text = "Reserva rebutjada: " + ex.Message; }
        }));
        panel.Children.Add(routeList);
        panel.Children.Add(Button("Alliberar itinerari seleccionat", (_, _) =>
        {
            if (routeList.SelectedItem is not string selected) return;
            try { _automation.ReleaseRoute(selected.Split('·')[0].Trim()); status.Text = "Itinerari alliberat."; Refresh(); }
            catch (Exception ex) { status.Text = ex.Message; }
        }));

        panel.Children.Add(Label("Avaluar senyal protegint un bloc"));
        panel.Children.Add(signalName);
        panel.Children.Add(protectedBlock);
        panel.Children.Add(Button("Calcular aspecte", (_, _) =>
        {
            try { status.Text = $"Senyal {signalName.Text}: {_automation.EvaluateSignal(signalName.Text, protectedBlock.Text)}"; Refresh(); }
            catch (Exception ex) { status.Text = ex.Message; }
        }));
        panel.Children.Add(Button("ATURADA D'EMERGÈNCIA ENCLAVADA", (_, _) =>
        {
            _automation.TriggerEmergencyStop("Activada des de Platinum WPF");
            status.Text = "Aturada enclavada. No es poden reservar itineraris."; Refresh();
        }));
        panel.Children.Add(Button("Reconèixer restabliment lògic", (_, _) =>
        {
            try { _automation.AcknowledgeEmergencyReset(true); status.Text = "Restabliment lògic reconegut; senyals a parada."; Refresh(); }
            catch (Exception ex) { status.Text = ex.Message; }
        }));
        panel.Children.Add(status);
        panel.Children.Add(Label("Motor lògic local. No és un enclavament ferroviari certificat i no comanda sortides físiques."));
        panel.Children.Add(Label("Registre d'esdeveniments"));
        panel.Children.Add(audit);
        Refresh();
        return Scroll(panel);
    }

    public void SetLayout(TrackLayout layout)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        RefreshDispatcher();
    }

    private void RefreshDispatcher()
    {
        if (_layout is null || _dispatcherBlocksList is null || _dispatcherRoutesList is null) return;
        _dispatcherBlocksList.ItemsSource = _layout.Blocks
            .Select(b => $"{(b.Occupied ? "● OCUPAT" : "○ LLIURE")} · {b.Name}")
            .ToArray();
        _dispatcherRoutesList.ItemsSource = _layout.Routes
            .Select(r => $"{(r.Reserved ? "RESERVAT" : "NO RESERVAT")} · {r.Name} · {string.Join(" → ", r.BlockIds.Select(id => _layout.Blocks.FirstOrDefault(b => b.Id == id)?.Name ?? "?"))}")
            .ToArray();
    }

    private void NotifyProjectChanged()
    {
        RefreshDispatcher();
        ProjectDataChanged?.Invoke(this, EventArgs.Empty);
    }

    private UIElement BuildDispatcher()
    {
        var root = TwoColumns();
        var left = Panel("Blocs lògics del projecte");
        var name = new TextBox { Text = "Bloc nou", MinWidth = 180, Margin = new Thickness(0, 4, 0, 8) };
        var list = new ListBox { MinHeight = 180, Background = Brush("#FF111820"), Foreground = Brushes.White };
        _dispatcherBlocksList = list;
        left.Children.Add(Label("Nom del bloc")); left.Children.Add(name);
        left.Children.Add(Button("Afegir bloc al projecte", (_, _) =>
        {
            if (_layout is null) { _dispatcherStatus.Text = "No hi ha cap projecte actiu."; return; }
            var value = name.Text.Trim();
            if (value.Length == 0 || _layout.Blocks.Any(b => string.Equals(b.Name, value, StringComparison.OrdinalIgnoreCase)))
            { _dispatcherStatus.Text = "Nom buit o bloc duplicat."; return; }
            var level = _layout.Levels.FirstOrDefault();
            if (level is null) { _dispatcherStatus.Text = "Crea primer un nivell al projecte."; return; }
            _layout.Blocks.Add(new LogicalBlock(Guid.NewGuid(), value, level.Id, Array.Empty<Guid>()));
            NotifyProjectChanged();
            _dispatcherStatus.Text = $"Bloc «{value}» desat al projecte actiu.";
        }));
        left.Children.Add(list);
        left.Children.Add(Button("Canviar lliure / ocupat", (_, _) =>
        {
            if (_layout is null || list.SelectedItem is not string selected) return;
            var value = selected[(selected.IndexOf('·') + 1)..].Trim();
            var index = _layout.Blocks.FindIndex(b => string.Equals(b.Name, value, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return;
            _layout.Blocks[index] = _layout.Blocks[index] with { Occupied = !_layout.Blocks[index].Occupied };
            NotifyProjectChanged();
            _dispatcherStatus.Text = $"Ocupació de «{value}» actualitzada al projecte. És un estat lògic/manual, no una lectura de sensor.";
        }));
        left.Children.Add(_dispatcherStatus);

        var right = Panel("Itineraris lògics del projecte");
        var routeName = new TextBox { Text = "Itinerari 1", Margin = new Thickness(0, 4, 0, 8) };
        var routeBlocks = new TextBox { Text = "", AcceptsReturn = true, MinHeight = 70, ToolTip = "Noms dels blocs separats per comes." };
        var routeList = new ListBox { MinHeight = 150, Background = Brush("#FF111820"), Foreground = Brushes.White };
        _dispatcherRoutesList = routeList;
        right.Children.Add(Label("Nom")); right.Children.Add(routeName); right.Children.Add(Label("Blocs (separats per comes)")); right.Children.Add(routeBlocks);
        right.Children.Add(Button("Crear itinerari al projecte", (_, _) =>
        {
            if (_layout is null) { _dispatcherStatus.Text = "No hi ha cap projecte actiu."; return; }
            var value = routeName.Text.Trim();
            var names = routeBlocks.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var blocks = names.Select(n => _layout.Blocks.FirstOrDefault(b => string.Equals(b.Name, n, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (value.Length == 0 || _layout.Routes.Any(r => string.Equals(r.Name, value, StringComparison.OrdinalIgnoreCase)) ||
                names.Length == 0 || blocks.Any(b => b is null) || blocks.Select(b => b!.Id).Distinct().Count() != blocks.Length)
            { _dispatcherStatus.Text = "Itinerari rebutjat: nom únic i blocs existents sense duplicats."; return; }
            _layout.Routes.Add(new RouteDefinition(Guid.NewGuid(), value, blocks.Select(b => b!.Id).ToArray()));
            NotifyProjectChanged();
            _dispatcherStatus.Text = $"Itinerari «{value}» desat al projecte.";
        }));
        right.Children.Add(routeList);
        right.Children.Add(Button("Reservar itinerari lògic", (_, _) =>
        {
            if (_layout is null || routeList.SelectedItem is not string selected) return;
            var namePart = selected.Split('·').Skip(1).FirstOrDefault()?.Trim();
            var index = _layout.Routes.FindIndex(r => r.Name == namePart);
            if (index < 0) return;
            var route = _layout.Routes[index];
            var blocks = route.BlockIds.Select(id => _layout.Blocks.FirstOrDefault(b => b.Id == id)).ToArray();
            if (blocks.Any(b => b is null || b.Occupied || _layout.Routes.Any(other => other.Id != route.Id && other.Reserved && other.BlockIds.Contains(b!.Id))))
            { _dispatcherStatus.Text = "Reserva rebutjada: hi ha blocs ocupats, inexistents o reservats."; return; }
            _layout.Routes[index] = route with { Reserved = true };
            NotifyProjectChanged();
            _dispatcherStatus.Text = "Reserva lògica desada. No activa cap desviament ni senyal físic.";
        }));
        right.Children.Add(Button("Alliberar reserva lògica", (_, _) =>
        {
            if (_layout is null || routeList.SelectedItem is not string selected) return;
            var namePart = selected.Split('·').Skip(1).FirstOrDefault()?.Trim();
            var index = _layout.Routes.FindIndex(r => r.Name == namePart);
            if (index < 0) return;
            _layout.Routes[index] = _layout.Routes[index] with { Reserved = false };
            NotifyProjectChanged();
            _dispatcherStatus.Text = "Reserva lògica alliberada.";
        }));
        right.Children.Add(Label("Les reserves i ocupacions són dades lògiques persistents. No constitueixen un enclavament certificat ni autoritzen sortides físiques."));
        AddColumn(root, left); AddColumn(root, right);
        RefreshDispatcher();
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
        panel.Children.Add(profiles);
        panel.Children.Add(Button("Provar connexió nativa Märklin CS2 (ping)", async (_, _) =>
        {
            if (!int.TryParse(port.Text, out var commandPort) || commandPort is < 1 or > 65535)
            {
                _stationStatus.Text = "Port CS2 invàlid. El valor habitual és 15731.";
                return;
            }

            MarklinCs2Adapter? candidate = null;
            try
            {
                if (_cs2Adapter is not null)
                {
                    await _cs2Adapter.DisposeAsync();
                    _cs2Adapter = null;
                }

                var profile = new ControlStationProfile("Märklin CS2", ControlStationKind.MarklinCs2, host.Text.Trim(), commandPort);
                candidate = (MarklinCs2Adapter)ControlStationAdapterFactory.Create(profile);
                _stationStatus.Text = "Enviant consulta de presència a la CS2...";
                await candidate.ConnectAsync();
                _cs2Adapter = candidate;
                candidate = null;
                _stationStatus.Text = "CS2 DETECTADA · protocol CAN natiu · ping confirmat. Tracció i accessoris continuen bloquejats.";
            }
            catch (Exception ex)
            {
                if (candidate is not null) await candidate.DisposeAsync();
                _stationStatus.Text = "No s'ha pogut confirmar la CS2: " + ex.Message;
            }
        }));
        panel.Children.Add(Button("Desconnectar CS2", async (_, _) =>
        {
            if (_cs2Adapter is not null)
            {
                await _cs2Adapter.DisposeAsync();
                _cs2Adapter = null;
            }
            _stationStatus.Text = "Adaptador CS2 desconnectat.";
        }));
        panel.Children.Add(_stationStatus);
        panel.Children.Add(Label("La prova utilitza UDP 15731 (comandes) i 15730 (respostes). Només envia ping; no activa la via ni mou locomotores o desviaments."));
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