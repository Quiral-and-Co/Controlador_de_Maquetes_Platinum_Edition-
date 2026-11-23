using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Platinum.Presentation.Avalonia.Infrastructure;
using Platinum.Presentation.Avalonia.ViewModels;

namespace Platinum.Presentation.Avalonia;

public sealed partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = new ServiceCollection().AddPresentation().BuildServiceProvider();
            desktop.MainWindow = new MainWindow(_services.GetRequiredService<MainWindowViewModel>());
            desktop.Exit += (_, _) => _services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}