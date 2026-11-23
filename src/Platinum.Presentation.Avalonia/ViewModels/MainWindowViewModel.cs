using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Platinum.Presentation.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private object _currentPage;
    private string _selectedSection = "Inici";

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
        _currentPage = services.GetRequiredService<DashboardViewModel>();
        NavigateHomeCommand = new RelayCommand(() => Navigate("Inici", services.GetRequiredService<DashboardViewModel>()));
        NavigateEditorCommand = new RelayCommand(() => Navigate("Editor 2D", services.GetRequiredService<LayoutEditorViewModel>()));
        NavigateCatalogueCommand = new RelayCommand(() => Navigate("Catàleg", services.GetRequiredService<CatalogueViewModel>()));
        NavigateOperationsCommand = new RelayCommand(() => Navigate("Operacions", services.GetRequiredService<OperationsViewModel>()));
    }

    public object CurrentPage { get => _currentPage; private set => SetProperty(ref _currentPage, value); }
    public string SelectedSection { get => _selectedSection; private set => SetProperty(ref _selectedSection, value); }
    public ICommand NavigateHomeCommand { get; }
    public ICommand NavigateEditorCommand { get; }
    public ICommand NavigateCatalogueCommand { get; }
    public ICommand NavigateOperationsCommand { get; }

    private void Navigate(string section, object page)
    {
        SelectedSection = section;
        CurrentPage = page;
    }
}