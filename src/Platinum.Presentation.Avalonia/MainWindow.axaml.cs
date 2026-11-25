using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Platinum.Application.Projects;
using Platinum.Domain.Layout;
using Platinum.Infrastructure.Projects;

namespace Platinum.Presentation.Avalonia;

public sealed partial class MainWindow : Window
{
    private readonly ProjectService _projects = new(new PlatinumPackageStore());
    private TrackLayout _currentLayout = CreateNewLayout("Nova maqueta");
    private string? _currentPath;
    private bool _isDirty = true;
    private bool _synchronizing;

    public MainWindow()
    {
        InitializeComponent();
        SynchronizeFields();
        SetStatus("Projecte nou. Encara no s'ha desat cap fitxer.");
    }

    private static TrackLayout CreateNewLayout(string name) => new()
    {
        Name = name,
        WidthMillimetres = 3000m,
        HeightMillimetres = 1800m,
        GridMillimetres = 10m,
        Levels = Enumerable.Range(0, 5)
            .Select(index => new TrackLevel(Guid.NewGuid(), $"Nivell {index + 1}", index * 110m))
            .ToList()
    };

    private void SynchronizeFields()
    {
        _synchronizing = true;
        try
        {
            ProjectNameBox.Text = _currentLayout.Name;
            WidthBox.Value = _currentLayout.WidthMillimetres;
            HeightBox.Value = _currentLayout.HeightMillimetres;
            GridBox.Value = _currentLayout.GridMillimetres;
            LevelsValue.Text = _currentLayout.Levels.Count.ToString();
            PiecesValue.Text = _currentLayout.Pieces.Count.ToString();
            RoutesValue.Text = _currentLayout.Routes.Count.ToString();
            FilePathText.Text = _currentPath ?? "Cap fitxer obert";
            DirtyText.Text = _isDirty ? "CANVIS PENDENTS DE DESAR" : "TOTS ELS CANVIS DESATS";
            Title = $"Quiral Platinum — {_currentLayout.Name}";
        }
        finally
        {
            _synchronizing = false;
        }
    }

    private void ProjectField_Changed(object? sender, TextChangedEventArgs e) => MarkDirty();

    private void ProjectNumber_Changed(object? sender, NumericUpDownValueChangedEventArgs e) => MarkDirty();

    private void MarkDirty()
    {
        if (_synchronizing)
            return;

        _currentLayout.Name = ProjectNameBox.Text?.Trim() ?? string.Empty;
        _currentLayout.WidthMillimetres = WidthBox.Value ?? 0m;
        _currentLayout.HeightMillimetres = HeightBox.Value ?? 0m;
        _currentLayout.GridMillimetres = GridBox.Value ?? 0m;
        _isDirty = true;
        DirtyText.Text = "CANVIS PENDENTS DE DESAR";
        Title = $"Quiral Platinum — {_currentLayout.Name} *";
    }

    private async void NewProject_Click(object? sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardChangesAsync())
            return;

        _currentLayout = CreateNewLayout("Nova maqueta");
        _currentPath = null;
        _isDirty = true;
        SynchronizeFields();
        SetStatus("Projecte nou creat en memòria. Desa'l per crear el fitxer .platinum.");
    }

    private async void OpenProject_Click(object? sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardChangesAsync())
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Obrir projecte Platinum",
            AllowMultiple = false,
            FileTypeFilter = [PlatinumFileType]
        });

        var file = files.FirstOrDefault();
        if (file is null)
            return;

        try
        {
            var path = file.Path.LocalPath;
            var loaded = await _projects.LoadAsync(path);
            _currentLayout = loaded;
            _currentPath = path;
            _isDirty = false;
            SynchronizeFields();
            SetStatus($"Projecte obert i validat correctament: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            SetStatus($"No s'ha pogut obrir el projecte: {ex.Message}");
            await ShowMessageAsync("Error en obrir el projecte", ex.Message);
        }
    }

    private async void SaveProject_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentPath))
        {
            await SaveAsAsync();
            return;
        }

        await SaveToPathAsync(_currentPath);
    }

    private async void SaveAsProject_Click(object? sender, RoutedEventArgs e) => await SaveAsAsync();

    private async Task SaveAsAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Desar projecte Platinum",
            SuggestedFileName = MakeSafeFileName(_currentLayout.Name) + ".platinum",
            DefaultExtension = "platinum",
            FileTypeChoices = [PlatinumFileType]
        });

        if (file is null)
            return;

        await SaveToPathAsync(file.Path.LocalPath);
    }

    private async Task SaveToPathAsync(string path)
    {
        try
        {
            ApplyFieldsToModel();
            await _projects.SaveAsync(_currentLayout, path);
            _currentPath = path;
            _isDirty = false;
            SynchronizeFields();
            SetStatus($"Projecte desat correctament: {path}");
        }
        catch (Exception ex)
        {
            SetStatus($"Error en desar el projecte: {ex.Message}");
            await ShowMessageAsync("No s'ha pogut desar el projecte", ex.Message);
        }
    }

    private async void ValidateProject_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ApplyFieldsToModel();
            var result = await _projects.ValidateAsync(_currentLayout);
            if (result.IsValid)
                SetStatus("VALIDACIÓ CORRECTA — " + result.Summary);
            else
                SetStatus("VALIDACIÓ FALLIDA — " + result.Summary);

            await ShowMessageAsync(result.IsValid ? "Projecte vàlid" : "Projecte no vàlid",
                result.Summary + (result.Issues.Count == 0 ? string.Empty : Environment.NewLine + string.Join(Environment.NewLine, result.Issues)));
        }
        catch (Exception ex)
        {
            SetStatus($"No s'ha pogut validar el projecte: {ex.Message}");
            await ShowMessageAsync("Error de validació", ex.Message);
        }
    }

    private void ApplyFieldsToModel()
    {
        _currentLayout.Name = ProjectNameBox.Text?.Trim() ?? string.Empty;
        _currentLayout.WidthMillimetres = WidthBox.Value ?? 0m;
        _currentLayout.HeightMillimetres = HeightBox.Value ?? 0m;
        _currentLayout.GridMillimetres = GridBox.Value ?? 0m;
    }

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!_isDirty)
            return true;

        var result = await ShowQuestionAsync(
            "Canvis pendents",
            "El projecte té canvis que encara no s'han desat. Vols descartar-los i continuar?");
        return result;
    }

    private async Task<bool> ShowQuestionAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var yes = new Button { Content = "Descartar canvis", MinWidth = 130 };
        var no = new Button { Content = "Continuar editant", MinWidth = 130 };
        var answer = false;
        yes.Click += (_, _) => { answer = true; dialog.Close(); };
        no.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { no, yes }
                }
            }
        };

        await dialog.ShowDialog(this);
        return answer;
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true
        };
        var close = new Button { Content = "Tanca", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        close.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                close
            }
        };
        await dialog.ShowDialog(this);
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private static string MakeSafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Where(character => !invalid.Contains(character)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "Nova-maqueta" : safe;
    }

    private static readonly FilePickerFileType PlatinumFileType = new("Projectes Platinum")
    {
        Patterns = ["*.platinum"],
        MimeTypes = ["application/vnd.quiral.platinum"]
    };
}
