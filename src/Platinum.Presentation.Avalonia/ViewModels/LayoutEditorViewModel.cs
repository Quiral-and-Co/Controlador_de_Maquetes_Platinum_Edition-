using Platinum.Presentation.Avalonia.Services;

namespace Platinum.Presentation.Avalonia.ViewModels;

public sealed class LayoutEditorViewModel(IDialogService dialogs) : PageViewModel(
    "Editor de maquetes 2D", "Preparat per migrar el model geomètric i les operacions d'edició sense alterar-ne les regles.")
{
    public async Task ExplainMigrationAsync() =>
        await dialogs.ShowMessageAsync("Editor 2D",
            "La superfície de dibuix i el model geomètric s'incorporaran en la fase següent. Encara no s'executen operacions físiques.");
}