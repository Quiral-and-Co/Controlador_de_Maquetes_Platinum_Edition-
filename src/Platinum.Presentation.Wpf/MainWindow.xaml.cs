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
        TrackEditor.CataloguePiecePlaced += OnCataloguePiecePlaced;
        TrackEditor.ConnectionCreated += OnConnectionCreated;
        TrackEditor.SetActiveLevel(ActiveLevelId);
        TrackEditor.SetGridSize(_currentLayout.GridMillimetres);
        TrackEditor.LoadSegments(_currentLayout.GeometrySegments);
        TrackEditor.LoadPieces(_currentLayout.Pieces);
        TrackEditor.LoadConnections(_currentLayout.Connections);
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
        TrackEditor.LoadPieces(_currentLayout.Pieces);
        TrackEditor.LoadConnections(_currentLayout.Connections);
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
            TrackEditor.LoadPieces(_currentLayout.Pieces);
            TrackEditor.LoadConnections(_currentLayout.Connections);
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
            _currentLayout.Pieces.Clear();
            _currentLayout.Pieces.AddRange(TrackEditor.Pieces);
            _currentLayout.Connections.Clear();
            _currentLayout.Connections.AddRange(TrackEditor.Connections);
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
        var service = new GitHubUpdateService();
        StatusText.Text = "Comprovant actualitzacions a GitHub...";
        try
        {
            var release = await service.GetLatestReleaseAsync();
            if (release is null)
            {
                MessageBox.Show(this, "No hi ha cap Release publicada. Les actualitzacions s'obtenen de les versions oficials del repositori.", "Actualitzacions", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = "No hi ha versions publicades.";
                return;
            }

            var current = typeof(MainWindow).Assembly.GetName().Version ?? new Version(0, 0);
            var latest = GitHubUpdateService.ParseVersion(release.Tag);
            var newer = latest > current;
            if (!newer)
            {
                MessageBox.Show(this, $"Platinum ja està actualitzat.\nVersió instal·lada: {current}\nVersió de GitHub: {release.Tag}", "Platinum actualitzat", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = "Platinum està actualitzat.";
                return;
            }

            if (release.Installer is null || release.Checksum is null)
            {
                MessageBox.Show(this, $"Hi ha una versió nova ({release.Tag}), però la Release encara no conté l'instal·lador verificable i el seu SHA-256.\n\n{release.Notes}", "Actualització pendent de publicació", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusText.Text = "Versió nova detectada; instal·lador no publicat.";
                return;
            }

            var confirm = MessageBox.Show(this,
                $"Nova versió: {release.Name} ({release.Tag})\nVersió actual: {current}\n\nEs descarregarà l'instal·lador oficial, se'n verificarà el SHA-256 i s'iniciarà la instal·lació. Platinum es tancarà.\n\n{release.Notes}\n\nVols continuar?",
                "Actualitzar Platinum des de GitHub", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            StatusText.Text = "Descarregant actualització oficial...";
            var installer = await service.DownloadAndVerifyInstallerAsync(release);
            StatusText.Text = "SHA-256 correcte. Iniciant instal·lador...";
            GitHubUpdateService.StartInstaller(installer);
            Application.Current.Shutdown();
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            MessageBox.Show(this, "Has cancel·lat l'autorització de Windows. No s'ha instal·lat cap actualització.", "Actualització cancel·lada", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Actualització cancel·lada.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No s'ha pogut completar l'actualització. La instal·lació actual no s'ha modificat.\n\n" + ex.Message, "Error d'actualització", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Error en actualitzar des de GitHub.";
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
            PlaceCatalogueButton.IsEnabled = false;
            TrackEditor.SetCatalogue(_catalogueItems);
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
        if (CatalogueList.SelectedItem is not CatalogueItem item) { PlaceCatalogueButton.IsEnabled = false; return; }
        PlaceCatalogueButton.IsEnabled = true;
        var geometry = string.IsNullOrWhiteSpace(item.GeometrySummary) ? "Geometria dimensional no disponible." : item.GeometrySummary;
        CatalogueDetails.Text = $"{item.DisplayName}\nCategoria: {item.Category}\nTipus: {item.Type}\nGeometria: {geometry}\nEstat: {item.GeometryStatus}" +
            (string.IsNullOrWhiteSpace(item.Notes) ? string.Empty : $"\nNota: {item.Notes}");
    }

    private void PlaceCatalogueButton_Click(object sender, RoutedEventArgs e)
    {
        if (CatalogueList.SelectedItem is not CatalogueItem item) return;
        if (item.Type == "special" && item.GeometryStatus == "unverified")
        {
            var answer = MessageBox.Show(this,
                $"La referència {item.Ref} és un conjunt o peça especial sense geometria verificada. Es col·locarà només com a marcador esquemàtic, no a escala. Vols continuar?",
                "Geometria no verificada", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }
        TrackEditor.BeginCataloguePlacement(item);
        StatusText.Text = $"Fes clic al plànol per col·locar {item.Ref} al nivell actiu.";
    }

    private void OnConnectionCreated(TrackConnection connection)
    {
        if (!_currentLayout.Connections.Any(existing => existing.Id == connection.Id))
            _currentLayout.Connections.Add(connection);
        StatusText.Text = "Connexió lògica creada. Cal verificar-la físicament abans de considerar-la real.";
        UpdateProjectStatus();
    }

    private void OnCataloguePiecePlaced(TrackPiece piece)
    {
        if (!_currentLayout.Pieces.Any(existing => existing.Id == piece.Id))
            _currentLayout.Pieces.Add(piece);
        StatusText.Text = $"Peça {piece.CatalogueCode} col·locada al nivell actiu. Desa el projecte per conservar els canvis.";
        UpdateProjectStatus();
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
