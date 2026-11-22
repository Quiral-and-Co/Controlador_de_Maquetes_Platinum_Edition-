using Microsoft.Win32;
using System.Windows;
using Platinum.Application.Projects;
using Platinum.Domain.Layout;
using Platinum.Infrastructure.Projects;

namespace Platinum.Presentation.Wpf;

public partial class MainWindow : Window
{
    private readonly ProjectService _projects = new(new PlatinumPackageStore());
    private TrackLayout _currentLayout = CreateNewLayout();
    private string? _currentPath;

    public MainWindow()
    {
        InitializeComponent();
        UpdateProjectStatus();
    }

    private static TrackLayout CreateNewLayout() => new()
    {
        Name = "Nova maqueta",
        Levels = [new TrackLevel(Guid.NewGuid(), "Nivell principal", 0m)]
    };

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        _currentLayout = CreateNewLayout();
        _currentPath = null;
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

    private void UpdateProjectStatus()
    {
        ProjectStatus.Text = _currentPath is null ? "PROJECTE NO DESAT" : "PROJECTE DESAT";
        Title = $"Quiral Platinum — {_currentLayout.Name}";
    }
}
