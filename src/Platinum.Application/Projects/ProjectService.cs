using Platinum.Domain.Layout;

namespace Platinum.Application.Projects;

public sealed class ProjectService(IProjectStore store)
{
    public Task SaveAsync(TrackLayout layout, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (string.IsNullOrWhiteSpace(layout.Name)) throw new ArgumentException("El projecte necessita un nom.", nameof(layout));
        if (layout.WidthMillimetres <= 0 || layout.HeightMillimetres <= 0 || layout.GridMillimetres <= 0)
            throw new InvalidOperationException("Les dimensions i la graella han de ser positives.");
        return store.SaveAsync(layout, path, cancellationToken);
    }

    public Task<TrackLayout> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Cal indicar el fitxer del projecte.", nameof(path));
        return store.LoadAsync(path, cancellationToken);
    }
}
