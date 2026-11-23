namespace Platinum.Presentation.Avalonia.ViewModels;

public abstract class PageViewModel(string title, string description) : ObservableObject
{
    public string Title { get; } = title;
    public string Description { get; } = description;
}