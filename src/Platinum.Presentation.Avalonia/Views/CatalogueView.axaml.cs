using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Platinum.Presentation.Avalonia.Views;

public sealed partial class CatalogueView : UserControl
{
    public CatalogueView() => AvaloniaXamlLoader.Load(this);
}