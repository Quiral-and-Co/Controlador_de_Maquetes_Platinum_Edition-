using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Platinum.Presentation.Wpf.Updates;

public sealed record GitHubRelease(string Tag, string Name, string Notes, Uri Page, Uri? Installer, long? InstallerBytes, Uri? Checksum);

public sealed class GitHubUpdateService
{
    private const string Owner = "Quiral-and-Co";
    private const string Repository = "Controlador_de_Maquetes_Platinum_Edition-";
    private static readonly HttpClient Client = CreateClient();
    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Quiral-Platinum-Wpf-Updater/1.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
    public async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = $"https://api.github.com/repos/{Owner}/{Repository}/releases/latest";
        using var response = await Client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? throw new InvalidDataException("Release sense tag.");
        var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? tag : tag;
        var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? string.Empty : string.Empty;
        var page = new Uri(root.GetProperty("html_url").GetString() ?? throw new InvalidDataException("Release sense URL."));
        Uri? installer = null; Uri? checksum = null; long? installerBytes = null;
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var file = asset.TryGetProperty("name", out var fn) ? fn.GetString() : null;
                if (string.IsNullOrWhiteSpace(file)) continue;
                var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps || !parsed.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) continue;
                if (file.EndsWith(".exe.sha256", StringComparison.OrdinalIgnoreCase)) checksum = parsed;
                if (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    installer = parsed;
                    installerBytes = asset.TryGetProperty("size", out var size) && size.TryGetInt64(out var bytes) ? bytes : null;
                }
            }
        }
        return new GitHubRelease(tag, name, notes, page, installer, installerBytes, checksum);
    }
    public async Task<string> DownloadAndVerifyInstallerAsync(GitHubRelease release, CancellationToken cancellationToken = default)
    {
        if (release.Installer is null || release.Checksum is null)
            throw new InvalidOperationException("La Release no inclou instal·lador i fitxer SHA-256.");
        ValidateReleaseAssetUrl(release.Installer);
        ValidateReleaseAssetUrl(release.Checksum);
        var safeTag = new string(release.Tag.Where(ch => char.IsAsciiLetterOrDigit(ch) || ch is '.' or '-' or '_').ToArray());
        if (string.IsNullOrWhiteSpace(safeTag) || !safeTag.Equals(release.Tag, StringComparison.Ordinal))
            throw new InvalidDataException("El tag de la Release conté caràcters no permesos.");
        var directory = Path.Combine(Path.GetTempPath(), "QuiralPlatinum", safeTag);
        Directory.CreateDirectory(directory);
        var installerPath = Path.Combine(directory, "Quiral-Platinum-Setup.exe");
        using (var response = await Client.GetAsync(release.Installer, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await source.CopyToAsync(destination, cancellationToken);
        }
        var checksumText = await Client.GetStringAsync(release.Checksum, cancellationToken);
        var expected = checksumText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (expected is null || expected.Length != 64 || !expected.All(Uri.IsHexDigit))
            throw new InvalidDataException("El manifest SHA-256 de GitHub no és vàlid.");
        await using var file = File.OpenRead(installerPath);
        var actual = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(file, cancellationToken)).ToLowerInvariant();
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(installerPath);
            throw new InvalidDataException("La verificació SHA-256 de l'instal·lador ha fallat. No s'executarà.");
        }
        return installerPath;
    }

    public static void StartInstaller(string installerPath)
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "QuiralPlatinum")) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(installerPath);
        if (!File.Exists(fullPath) || !fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ruta d'instal·lador temporal no vàlida.");
        Process.Start(new ProcessStartInfo(fullPath,
            "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS")
        { UseShellExecute = true, Verb = "runas" });
    }

    private static void ValidateReleaseAssetUrl(Uri url)
    {
        if (url.Scheme != Uri.UriSchemeHttps ||
            (!url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
             !url.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("L'actiu no prové d'un domini oficial de GitHub.");
    }

    public static Version ParseVersion(string tag)
    {
        var value = tag.Trim(); if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase)) value = value[1..];
        var suffix = value.IndexOfAny(['-', '+']); if (suffix >= 0) value = value[..suffix];
        return Version.TryParse(value, out var version) ? version : new Version(0, 0);
    }
    public static void OpenOfficialUrl(Uri url)
    {
        if (url.Scheme != Uri.UriSchemeHttps || !url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Només s'accepten URLs HTTPS de github.com.");
        Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }
}