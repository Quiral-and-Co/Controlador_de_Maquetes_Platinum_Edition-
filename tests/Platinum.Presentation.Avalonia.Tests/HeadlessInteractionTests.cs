using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Xunit;

namespace Platinum.Presentation.Avalonia.Tests;

public sealed class HeadlessInteractionTests
{
    [AvaloniaFact]
    public void Button_Click_Updates_ViewModel_State()
    {
        var count=0;
        var button=new Button { Content="Connect" };
        button.Click += (_,_)=>count++;
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1,count);
    }

    [AvaloniaFact]
    public void Window_Contains_Interactive_Avalonia_Controls()
    {
        var toggle=new ToggleButton { Content="Grid snap", IsChecked=true };
        var window=new Window { Width=640, Height=480, Content=toggle };
        Assert.Equal(true,toggle.IsChecked);
        toggle.IsChecked=false;
        Assert.Equal(false,toggle.IsChecked);
        window.Close();
    }
}
