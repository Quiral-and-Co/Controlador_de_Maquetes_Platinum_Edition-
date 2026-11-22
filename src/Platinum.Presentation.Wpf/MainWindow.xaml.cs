using Microsoft.Win32;
using System.Windows;
using Platinum.Application.Projects;
using Platinum.Domain.Layout;
using Platinum.Infrastructure.Projects;
using Platinum.Presentation.Wpf.Updates;
using Platinum.Presentation.Wpf.Editor;
using Platinum.Presentation.Wpf.Catalogue;

namespace Platinum.Presentation.Wpf;

public partial class MainWindow : Window
{
    private readonly ProjectService _projects = new(new PlatinumPackageStore());
    private TrackLayout _currentLayout = CreateNewLayout();
    private string? _currentPath;
    private List<CatalogueItem> _catalogueItems = [];

    public MainWindow()
    {
        InitializeComponent();
        LoadCatalogue();
        TrackEditor.SetActiveLevel(ActiveLevelId);
        TrackEditor.SetGridSize(_currentLayout.GridMillimetres);
        TrackEditor.LoadSegments(_currentLayout.GeometrySegments);
        RefreshLevelSelector();
        UpdateProjectStatus();
    }

    private static TrackLayout CreateNewLayout() => new()
    {
        Name = "Nova maqueta",
        Levels = Enumerable.Range(0, 5).Select(i => new TrackLevel(Guid.NewGuid(), "Nivell " + (i + 1), i * 110m)).ToList()
    };

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        _currentLayout = CreateNewLayout();
        _currentPath = null;
        TrackEditor.SetActiveLevel(ActiveLevelId);
        TrackEditor.SetGridSize(_currentLayout.GridMillimetres);
        TrackEditor.LoadSegments(_currentLayout.GeometrySegments);
        RefreshLevelSelector();
        UpdateProjectStatus();
        StatusText.Text = "Projecte nou creat. Encara no s'ha desat.";
    }

    private async void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Obrir projecte Platinum",
            Filter = "Projecte Quiral Platinum (*.platinum)|*.platinum",
            DefaultExt = ".platinum",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            _currentLayout = await _projects.LoadAsync(dialog.FileName);
            _currentPath = dialog.FileName;
            if (_currentLayout.Levels.Count == 0) _currentLayout.Levels.Add(new TrackLevel(Guid.NewGuid(), "Nivell principal", 0m));
            TrackEditor.SetActiveLevel(ActiveLevelId);
            TrackEditor.SetGridSize(_currentLayout.GridMillimetres);
            TrackEditor.LoadSegments(_currentLayout.GeometrySegments);
            RefreshLevelSelector();
            UpdateProjectStatus();
            StatusText.Text = "Projecte carregat i validat.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "No s'ha pogut obrir el projecte", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Error en obrir el projecte.";
        }
    }

    private async void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        var path = _currentPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Title = "Desar projecte Platinum",
                Filter = "Projecte Quiral Platinum (*.platinum)|*.platinum",
                DefaultExt = ".platinum",
                AddExtension = true,
                FileName = _currentLayout.Name
            };
            if (dialog.ShowDialog(this) != true) return;
            path = dialog.FileName;
        }

        try
        {
            _currentLayout.GeometrySegments = TrackEditor.Segments.ToList();
            await _projects.SaveAsync(_currentLayout, path);
            _currentPath = path;
            UpdateProjectStatus();
            StatusText.Text = "Projecte desat correctament.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "No s'ha pogut desar el projecte", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Error en desar el projecte.";
        }
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Consultant les versions publicades a GitHub…";
        try
        {
            var service = new GitHubUpdateService();
            var release = await service.GetLatestReleaseAsync();
            if (release is null)
            {
                MessageBox.Show(this, "Encara no hi ha cap Release publicada al repositori. Quan es publiqui una versió amb un instal·lador .exe, apareixerà aquí.", "Actualitzacions", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = "No hi ha cap Release publicada.";
                return;
            }
            var current = typeof(MainWindow).Assembly.GetName().Version ?? new Version(0, 0);
            var latest = GitHubUpdateService.ParseVersion(release.Tag);
            var isNewer = latest > current;
            var message = release.Name + " (" + release.Tag + ")\n\n" +
                (isNewer ? "Hi ha una versió més nova disponible.\n\n" : "Aquesta és la versió publicada més recent.\n\n") +
                (release.Installer is null ? "La Release no conté cap instal·lador .exe. Consulta les notes a GitHub." : "Instal·lador .exe disponible a GitHub." ) +
                (string.IsNullOrWhiteSpace(release.Notes) ? string.Empty : "\n\nNotes:\n" + release.Notes);
            var answer = MessageBox.Show(this, message + "\n\nVols obrir la descàrrega oficial?", "Actualitzacions de Platinum", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
                GitHubUpdateService.OpenOfficialUrl(isNewer && release.Installer is not null ? release.Installer : release.Page);
            StatusText.Text = isNewer ? "Actualització disponible: " + release.Tag : "Versió més recent: " + release.Tag;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No s'ha pogut consultar GitHub.\n\n" + ex.Message, "Error en cercar actualitzacions", MessageBoxButton.OK, MessageBoxImage.Warning);
            StatusText.Text = "No s'ha pogut consultar GitHub.";
        }
    }

    private void LoadCatalogue()
    {
        try
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "catalogue-marklin-c.json");
            var catalogue = MarklinCCatalogue.Load(path);
            _catalogueItems = catalogue.Items;
            CatalogueList.ItemsSource = _catalogueItems;
            CatalogueCount.Text = $"{_catalogueItems.Count} referències · {catalogue.Brand} {catalogue.System} · {catalogue.Scale}";
        }
        catch (Exception ex)
        {
            _catalogueItems = [];
            CatalogueCount.Text = "No s'ha pogut carregar el catàleg.";
            CatalogueDetails.Text = ex.Message;
        }
    }

    private void CatalogueSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (CatalogueList is null) return;
        var query = CatalogueSearch.Text.Trim();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _catalogueItems
            : _catalogueItems.Where(item =>
                item.Ref.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Category.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        CatalogueList.ItemsSource = filtered;
        CatalogueCount.Text = $"{filtered.Count} de {_catalogueItems.Count} referències";
    }

    private void CatalogueList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CatalogueList.SelectedItem is not CatalogueItem item) return;
        var geometry = string.IsNullOrWhiteSpace(item.GeometrySummary) ? "Geometria dimensional no disponible." : item.GeometrySummary;
        CatalogueDetails.Text = $"{item.DisplayName}\nCategoria: {item.Category}\nTipus: {item.Type}\nGeometria: {geometry}\nEstat: {item.GeometryStatus}" +
            (string.IsNullOrWhiteSpace(item.Notes) ? string.Empty : $"\nNota: {item.Notes}");
    }

    private Guid ActiveLevelId => LevelSelector.SelectedValue is Guid selected ? selected : _currentLayout.Levels.FirstOrDefault()?.Id ?? Guid.Empty;

    private void RefreshLevelSelector()
    {
        LevelSelector.ItemsSource = _currentLayout.Levels;
        LevelSelector.DisplayMemberPath = nameof(TrackLevel.Name);
        LevelSelector.SelectedValuePath = nameof(TrackLevel.Id);
        LevelSelector.SelectedValue = _currentLayout.Levels.FirstOrDefault()?.Id;
        TrackEditor.SetActiveLevel(ActiveLevelId);
    }

    private void LevelSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TrackEditor is null || LevelSelector.SelectedValue is not Guid levelId) return;
        TrackEditor.SetActiveLevel(levelId);
        StatusText.Text = "Editant: " + (_currentLayout.Levels.FirstOrDefault(x => x.Id == levelId)?.Name ?? "Nivell");
    }

    private void UpdateProjectStatus()
    {
        ProjectStatus.Text = _currentPath is null ? "PROJECTE NO DESAT" : "PROJECTE DESAT";
        Title = $"Quiral Platinum — {_currentLayout.Name}";
    }
}
