using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Platinum.Presentation.Avalonia.Services;

public sealed class AvaloniaDialogService : IDialogService
{
    private Window? Owner => Application.Current?.ApplicationLifetime is
        Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

    public async Task ShowMessageAsync(string title, string message)
    {
        var dialog = CreateDialog(title, message, false);
        await dialog.ShowDialog(Owner!);
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = CreateDialog(title, message, true);
        return await dialog.ShowDialog<bool>(Owner!);
    }

    private static Window CreateDialog(string title, string message, bool confirm)
    {
        var dialog = new Window
        {
            Title = title, Width = 440, SizeToContent = SizeToContent.Height,
            CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Padding = new Thickness(22)
        };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { Content = confirm ? "Sí" : "D'acord", MinWidth = 90 };
        ok.Click += (_, _) => dialog.Close(true);
        buttons.Children.Add(ok);
        if (confirm)
        {
            var cancel = new Button { Content = "Cancel·la", MinWidth = 90 };
            cancel.Click += (_, _) => dialog.Close(false);
            buttons.Children.Insert(0, cancel);
        }
        dialog.Content = new StackPanel { Spacing = 18, Children =
        {
            new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14 },
            buttons
        }};
        return dialog;
    }
}