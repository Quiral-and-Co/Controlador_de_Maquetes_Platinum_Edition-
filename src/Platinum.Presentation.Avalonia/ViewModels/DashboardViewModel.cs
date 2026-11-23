namespace Platinum.Presentation.Avalonia.ViewModels;

public sealed class DashboardViewModel() : PageViewModel(
    "Centre de control", "Entorn de treball de Quiral Platinum. La capa de negoci i el domini continuen compartits amb la versió WPF.")
{
    public string MigrationStatus => "Shell Avalonia · MVVM · navegació i serveis";
    public string SafetyStatus => "Les ordres físiques continuen bloquejades durant la migració.";
}