using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Headless.XUnit;
using Platinum.Presentation.Avalonia;
using Xunit;

namespace Platinum.Presentation.Avalonia.Tests;

public sealed class MainWindowSmokeTests
{
    [AvaloniaFact]
    public void MainWindow_Loads_Core_Editor_And_Catalogue()
    {
        var window = new MainWindow();
        try
        {
            Assert.Equal("Quiral Platinum — Maquetisme ferroviari", window.Title);

            var levels = window.FindControl<ComboBox>("LevelSelector");
            var catalogue = window.FindControl<ListBox>("CatalogueList");
            var place = window.FindControl<Button>("PlaceCatalogueButton");
            var status = window.FindControl<TextBlock>("ProjectStatus");

            Assert.NotNull(levels);
            Assert.Equal(5, levels!.Items.Count);
            Assert.NotNull(catalogue);
            Assert.Equal(68, catalogue!.Items.Count);
            Assert.NotNull(place);
            Assert.False(place!.IsEnabled);
            Assert.NotNull(status);
            Assert.Equal("PROJECTE NO DESAT", status!.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void New_Project_Action_Resets_Project_Status()
    {
        var window = new MainWindow();
        try
        {
            var status = window.FindControl<TextBlock>("StatusText");
            Assert.NotNull(status);

            var newProject = window.FindControl<Button>("NewProjectButton");
            Assert.NotNull(newProject);
            newProject!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal("Projecte nou creat.", status!.Text);
        }
        finally
        {
            window.Close();
        }
    }
}
