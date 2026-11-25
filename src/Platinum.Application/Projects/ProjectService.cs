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

    /// <summary>
    /// Validates the project through the real package serializer without changing
    /// the user's project file. The temporary package is always removed.
    /// </summary>
    public async Task<ProjectValidationResult> ValidateAsync(
        TrackLayout layout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var temporaryPath = Path.Combine(
            Path.GetTempPath(),
            $"platinum-validation-{Guid.NewGuid():N}.platinum");

        try
        {
            await SaveAsync(layout, temporaryPath, cancellationToken);
            return new ProjectValidationResult(
                true,
                "El projecte supera les validacions del domini i s'ha pogut serialitzar en format .platinum.",
                Array.Empty<string>());
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
                                          or InvalidDataException or IOException
                                          or UnauthorizedAccessException)
        {
            return new ProjectValidationResult(false, exception.Message, [exception.Message]);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                // A temporary validation artifact must never mask the validation result.
            }
            catch (UnauthorizedAccessException)
            {
                // Same as above; the OS may temporarily hold the file.
            }
        }
    }
}

public sealed record ProjectValidationResult(
    bool IsValid,
    string Summary,
    IReadOnlyList<string> Issues);
