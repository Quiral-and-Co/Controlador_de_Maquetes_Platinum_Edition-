using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Platinum.Presentation.Avalonia.Views;

public sealed partial class DashboardView : UserControl
{
    public DashboardView() => AvaloniaXamlLoader.Load(this);
}