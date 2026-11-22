using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Platinum.Domain.Layout;

namespace Platinum.Presentation.Wpf;

/// <summary>
/// Edició de dades persistents del projecte. Els registres físics són declaracions d'operador,
/// mai una certificació ni una autorització d'enviament de comandes.
/// </summary>
public sealed class ProjectDataPanelsView : UserControl
{
    private TrackLayout? _layout;
    private readonly StackPanel _levels = Section("Nivells i cotes");
    private readonly StackPanel _sensors = Section("Sensors i blocs");
    private readonly StackPanel _signals = Section("Senyalització");
    private readonly StackPanel _locomotives = Section("Cabina de locomotores · perfils locals");
    private readonly StackPanel _inspections = Section("Validació física · registre d'inspecció");
    private readonly TextBlock _status = new() { Foreground = Brushes.Gold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) };

    public ProjectDataPanelsView()
    {
        var tabs = new TabControl { Background = Brush("#FF18212B"), Foreground = Brushes.White };
        tabs.Items.Add(new TabItem { Header = "Nivells", Content = Scroll(_levels) });
        tabs.Items.Add(new TabItem { Header = "Sensors i blocs", Content = Scroll(_sensors) });
        tabs.Items.Add(new TabItem { Header = "Senyals", Content = Scroll(_signals) });
        tabs.Items.Add(new TabItem { Header = "Locomotores", Content = Scroll(_locomotives) });
        tabs.Items.Add(new TabItem { Header = "Validació física", Content = Scroll(_inspections) });
        var root = new DockPanel();
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(_status);
        root.Children.Add(tabs);
        Content = root;
    }

    public void SetLayout(TrackLayout layout)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Render();
    }

    public void ApplyToLayout(TrackLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!ReferenceEquals(layout, _layout)) throw new InvalidOperationException("Els panells no estan vinculats al projecte actiu.");
        // Cada editor desa directament a la mateixa instància TrackLayout.
    }

    private void Render()
    {
        if (_layout is null) return;
        _levels.Children.Clear(); _sensors.Children.Clear(); _signals.Children.Clear(); _locomotives.Children.Clear(); _inspections.Children.Clear();

        var levelName = Input("Nom del nivell");
        var elevation = Input("Cota (mm)", "0");
        Add(_levels, "Afegir nivell", () =>
        {
            if (string.IsNullOrWhiteSpace(levelName.Text) || !decimal.TryParse(elevation.Text, out var z)) { SetStatus("Nom o cota invàlids."); return; }
            _layout.Levels.Add(new TrackLevel(Guid.NewGuid(), levelName.Text.Trim(), z));
            Render();
        });
        foreach (var level in _layout.Levels)
            _levels.Children.Add(Label($"{level.Name} · cota {level.ElevationMillimetres:0.##} mm"));

        var sensorSystem = Input("Identificador del sensor (p. ex. S88:1)");
        var sensorName = Input("Nom visible del sensor");
        Add(_sensors, "Afegir sensor", () =>
        {
            if (string.IsNullOrWhiteSpace(sensorSystem.Text) || string.IsNullOrWhiteSpace(sensorName.Text) ||
                _layout.Sensors.Any(s => string.Equals(s.SystemName, sensorSystem.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
            { SetStatus("Sensor invàlid o identificador duplicat."); return; }
            _layout.Sensors.Add(new FeedbackSensor(Guid.NewGuid(), sensorSystem.Text.Trim(), sensorName.Text.Trim(), false));
            Render();
        });
        foreach (var sensor in _layout.Sensors)
            _sensors.Children.Add(Label($"{sensor.DisplayName} · {sensor.SystemName} · {(sensor.PhysicallyVerified ? "verificat" : "no verificat")}"));
        _sensors.Children.Add(Label($"Blocs del projecte: {_layout.Blocks.Count}. Es gestionen des del Dispatcher d'Operacions."));

        var signalName = Input("Nom del senyal");
        var aspect = new ComboBox { ItemsSource = new[] { "unknown", "stop", "proceed", "caution" }, SelectedIndex = 0, Width = 220, Margin = new Thickness(8) };
        var block = new ComboBox { ItemsSource = _layout.Blocks, DisplayMemberPath = nameof(LogicalBlock.Name), SelectedIndex = _layout.Blocks.Count > 0 ? 0 : -1, Width = 260, Margin = new Thickness(8) };
        _signals.Children.Add(Label("Aspecte lògic/simulat. No es transmet a cap descodificador."));
        _signals.Children.Add(signalName); _signals.Children.Add(aspect); _signals.Children.Add(block);
        Add(_signals, "Afegir senyal", () =>
        {
            if (string.IsNullOrWhiteSpace(signalName.Text)) { SetStatus("El senyal necessita un nom."); return; }
            var selected = block.SelectedItem as LogicalBlock;
            _layout.Signals.Add(new SignalDefinition(Guid.NewGuid(), signalName.Text.Trim(), aspect.SelectedItem?.ToString() ?? "unknown", selected?.Id, false));
            Render();
        });
        foreach (var signal in _layout.Signals)
            _signals.Children.Add(Label($"{signal.Name} · {signal.Aspect} · bloc: {_layout.Blocks.FirstOrDefault(b => b.Id == signal.ProtectedBlockId)?.Name ?? "cap"} · sortida física bloquejada"));

        var locoName = Input("Nom de locomotora");
        var address = Input("Adreça DCC (1–10239)", "3");
        var protocol = new ComboBox { ItemsSource = new[] { "simulation", "Märklin CS2 (només perfil)", "Z21 (només perfil)", "ESU ECoS (només perfil)", "DCC genèric (només perfil)" }, SelectedIndex = 0, Width = 280, Margin = new Thickness(8) };
        _locomotives.Children.Add(locoName); _locomotives.Children.Add(address); _locomotives.Children.Add(protocol);
        _locomotives.Children.Add(Label("Aquests perfils són dades del projecte; no connecten amb centrals ni envien ordres."));
        Add(_locomotives, "Afegir perfil de locomotora", () =>
        {
            if (string.IsNullOrWhiteSpace(locoName.Text) || !int.TryParse(address.Text, out var number) || number is < 1 or > 10239)
            { SetStatus("Nom o adreça de locomotora invàlids."); return; }
            _layout.Locomotives.Add(new LocomotiveProfile(Guid.NewGuid(), locoName.Text.Trim(), number, protocol.SelectedItem?.ToString() ?? "simulation", true));
            Render();
        });
        foreach (var loco in _layout.Locomotives)
            _locomotives.Children.Add(Label($"{loco.Name} · adreça {loco.Address} · {loco.Protocol} · SIMULACIÓ"));

        _inspections.Children.Add(Label("Marca només comprovacions que hagis inspeccionat. Es desa qui/què s'ha declarat, però no desbloqueja les sortides."));
        foreach (var inspection in _layout.Inspections)
        {
            var row = new StackPanel { Margin = new Thickness(8) };
            row.Children.Add(Label($"{inspection.Item} · {(inspection.OperatorDeclaredComplete ? "declarat complet" : "pendent")} · {inspection.CheckedUtc.LocalDateTime:g}"));
            if (!string.IsNullOrWhiteSpace(inspection.Notes)) row.Children.Add(Label(inspection.Notes));
            _inspections.Children.Add(row);
        }
        foreach (var item in new[] { "Traçat mecànic", "Connexions elèctriques", "Sensors i blocs", "Desviaments", "Parada d'emergència física", "Alimentació i proteccions" })
        {
            var check = new CheckBox { Content = item, Foreground = Brushes.White, Margin = new Thickness(8) };
            _inspections.Children.Add(check);
            var notes = Input("Notes de la inspecció (opcional)");
            _inspections.Children.Add(notes);
            Add(_inspections, "Desar declaració", () =>
            {
                _layout.Inspections.Add(new PhysicalInspectionRecord(Guid.NewGuid(), item, check.IsChecked == true, notes.Text.Trim(), DateTimeOffset.UtcNow));
                Render();
            });
        }
        SetStatus("Dades vinculades al projecte actiu. Sortides físiques: BLOQUEJADES.");
    }

    private void SetStatus(string message) => _status.Text = message + " · Les declaracions no són certificació ferroviària.";
    private static StackPanel Section(string title)
    {
        var panel = new StackPanel { Margin = new Thickness(14), MaxWidth = 850 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 19, FontWeight = FontWeights.Bold, Foreground = Brush("#FFFFC65C"), Margin = new Thickness(8) });
        return panel;
    }
    private static TextBox Input(string hint, string value = "")
    {
        var box = new TextBox { Text = value, Width = 280, Padding = new Thickness(7), Margin = new Thickness(8), ToolTip = hint };
        return box;
    }
    private static TextBlock Label(string value) => new() { Text = value, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) };
    private static void Add(Panel panel, string caption, Action action)
    {
        var button = new Button { Content = caption, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => action();
        panel.Children.Add(button);
    }
    private static ScrollViewer Scroll(UIElement content) => new() { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
