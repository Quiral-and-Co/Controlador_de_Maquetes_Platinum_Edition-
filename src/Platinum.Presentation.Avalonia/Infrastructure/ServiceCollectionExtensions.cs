using Microsoft.Extensions.DependencyInjection;
using Platinum.Presentation.Avalonia.Services;
using Platinum.Presentation.Avalonia.ViewModels;

namespace Platinum.Presentation.Avalonia.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<LayoutEditorViewModel>();
        services.AddSingleton<CatalogueViewModel>();
        services.AddSingleton<OperationsViewModel>();
        return services;
    }
}