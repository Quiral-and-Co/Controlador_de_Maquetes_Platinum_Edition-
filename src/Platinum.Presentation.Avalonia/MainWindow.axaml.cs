using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Platinum.Presentation.Avalonia.ViewModels;

namespace Platinum.Presentation.Avalonia;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = viewModel;
    }
}