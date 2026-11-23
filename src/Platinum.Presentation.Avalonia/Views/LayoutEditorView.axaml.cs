using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Platinum.Presentation.Avalonia.ViewModels;

namespace Platinum.Presentation.Avalonia.Views;

public sealed partial class LayoutEditorView : UserControl
{
    public LayoutEditorView() => AvaloniaXamlLoader.Load(this);
}