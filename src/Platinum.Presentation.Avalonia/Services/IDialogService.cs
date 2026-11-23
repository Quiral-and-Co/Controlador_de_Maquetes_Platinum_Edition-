using Avalonia.Platform.Storage;

namespace Platinum.Presentation.Avalonia.Services;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message);
    Task<string?> OpenFileAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypes = null);
    Task<string?> SaveFileAsync(string title, string suggestedFileName,
        IReadOnlyList<FilePickerFileType>? fileTypes = null);
}