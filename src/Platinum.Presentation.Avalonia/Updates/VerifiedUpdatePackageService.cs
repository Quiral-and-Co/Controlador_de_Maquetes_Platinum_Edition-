using System.IO.Compression;
using System.Security.Cryptography;

namespace Platinum.Presentation.Avalonia.Updates;

/// <summary>
/// Baixa un paquet ZIP autenticat amb SHA-256, l'expandeix a staging i el promou amb còpia
/// de recuperació. No substitueix fitxers en ús: l'aplicació ha de cridar-ho des d'un updater extern.
/// </summary>
public sealed class VerifiedUpdatePackageService(HttpClient? client = null) : IDisposable
{
    private readonly HttpClient _http = client ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    private readonly bool _ownsClient = client is null;

    public async Task StageAndInstallAsync(Uri packageUri, Uri checksumUri, string installDirectory, CancellationToken cancellationToken = default)
    {
        ValidateDownloadUri(packageUri); ValidateDownloadUri(checksumUri);
        if (string.IsNullOrWhiteSpace(installDirectory)) throw new ArgumentException("Directori d'instal·lació obligatori.", nameof(installDirectory));
        var destination = Path.GetFullPath(installDirectory);
        var parent = Directory.GetParent(destination)?.FullName ?? throw new InvalidOperationException("Directori d'instal·lació sense pare.");
        Directory.CreateDirectory(parent);
        var id = Path.GetFileName(destination);
        var archive = Path.Combine(parent, id + ".update.zip");
        var checksum = Path.Combine(parent, id + ".update.sha256");
        var staging = Path.Combine(parent, id + ".staging");
        var backup = Path.Combine(parent, id + ".backup");
        try
        {
            await DownloadAsync(packageUri, archive, cancellationToken).ConfigureAwait(false);
            var manifest = await _http.GetStringAsync(checksumUri, cancellationToken).ConfigureAwait(false);
            var expected = manifest.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant();
            if (expected is null || expected.Length != 64 || !expected.All(Uri.IsHexDigit))
                throw new InvalidDataException("Manifest SHA-256 invàlid.");
            await using (var input = File.OpenRead(archive))
            {
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false)).ToLowerInvariant();
                if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected), Convert.FromHexString(actual)))
                    throw new InvalidDataException("El paquet d'actualització no supera la verificació SHA-256.");
            }
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);
            ExtractZipSafely(archive, staging);
            if (!Directory.EnumerateFileSystemEntries(staging).Any()) throw new InvalidDataException("Paquet buit.");
            if (Directory.Exists(backup)) Directory.Delete(backup, true);
            if (Directory.Exists(destination)) Directory.Move(destination, backup);
            try
            {
                Directory.Move(staging, destination);
                if (Directory.Exists(backup)) Directory.Delete(backup, true);
            }
            catch
            {
                if (!Directory.Exists(destination) && Directory.Exists(backup)) Directory.Move(backup, destination);
                throw;
            }
        }
        finally
        {
            if (File.Exists(archive)) File.Delete(archive);
            if (File.Exists(checksum)) File.Delete(checksum);
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    /// <summary>Recupera una instal·lació interrompuda entre el backup i la promoció.</summary>
    public static void Recover(string installDirectory)
    {
        var destination = Path.GetFullPath(installDirectory);
        var parent = Directory.GetParent(destination)?.FullName ?? throw new InvalidOperationException("Directori sense pare.");
        var backup = Path.Combine(parent, Path.GetFileName(destination) + ".backup");
        if (!Directory.Exists(destination) && Directory.Exists(backup)) Directory.Move(backup, destination);
    }

    private async Task DownloadAsync(Uri uri, string path, CancellationToken token)
    {
        using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        await using var target = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await source.CopyToAsync(target, token).ConfigureAwait(false);
        await target.FlushAsync(token).ConfigureAwait(false);
    }

    private static void ValidateDownloadUri(Uri uri)
    {
        if (uri is null || uri.Scheme != Uri.UriSchemeHttps ||
            !(uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
              uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase) ||
              uri.Host.Equals("release-assets.githubusercontent.com", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Només s'accepten URL HTTPS d'assets oficials de GitHub.");
    }

    private static void ExtractZipSafely(string archive, string destination)
    {
        var root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        using var zip = ZipFile.OpenRead(archive);
        foreach (var entry in zip.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!target.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new InvalidDataException("El paquet conté una ruta fora del directori d'instal·lació.");
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal)) { Directory.CreateDirectory(target); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    public void Dispose() { if (_ownsClient) _http.Dispose(); }
}
