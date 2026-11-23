using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace Platinum.Presentation.Avalonia.Services;

public sealed class AvaloniaDialogService : IDialogService
{
    private Window? Owner => Avalonia.Application.Current?.ApplicationLifetime is
        IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;

    public async Task ShowMessageAsync(string title, string message)
    {
        var owner = RequireOwner();
        var dialog = CreateDialog(title, message, false);
        await dialog.ShowDialog(owner);
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var owner = RequireOwner();
        var dialog = CreateDialog(title, message, true);
        return await dialog.ShowDialog<bool>(owner);
    }

    public async Task<string?> OpenFileAsync(string title,
        IReadOnlyList<FilePickerFileType>? fileTypes = null)
    {
        var owner = RequireOwner();
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title, AllowMultiple = false, FileTypeFilter = fileTypes
        });
        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }

    public async Task<string?> SaveFileAsync(string title, string suggestedFileName,
        IReadOnlyList<FilePickerFileType>? fileTypes = null)
    {
        var owner = RequireOwner();
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title, SuggestedFileName = suggestedFileName, FileTypeChoices = fileTypes
        });
        return file?.Path.LocalPath;
    }

    private Window RequireOwner() => Owner ??
        throw new InvalidOperationException("Els diàlegs només es poden obrir quan la finestra principal és activa.");

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