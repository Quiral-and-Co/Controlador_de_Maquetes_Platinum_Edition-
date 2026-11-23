using System.Windows.Input;
using Platinum.Presentation.Avalonia.Services;

namespace Platinum.Presentation.Avalonia.ViewModels;

public sealed class LayoutEditorViewModel : PageViewModel
{
    private readonly IDialogService _dialogs;
    public LayoutEditorViewModel(IDialogService dialogs) : base(
        "Editor de maquetes 2D", "Preparat per migrar el model geomètric i les operacions d'edició sense alterar-ne les regles.")
    {
        _dialogs = dialogs;
        ExplainMigrationCommand = new RelayCommand(() => _ = ExplainMigrationAsync());
    }

    public ICommand ExplainMigrationCommand { get; }

    private Task ExplainMigrationAsync() =>
        _dialogs.ShowMessageAsync("Editor 2D",
            "La superfície de dibuix i el model geomètric s'incorporaran en la fase següent. Encara no s'executen operacions físiques.");
}