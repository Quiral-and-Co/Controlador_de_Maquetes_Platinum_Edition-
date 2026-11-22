using Platinum.Domain.Layout;

namespace Platinum.Application.Projects;

public interface IProjectStore
{
    Task SaveAsync(TrackLayout layout, string path, CancellationToken cancellationToken = default);
    Task<TrackLayout> LoadAsync(string path, CancellationToken cancellationToken = default);
}
