using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Platinum.Presentation.Wpf.Updates;

public sealed record GitHubRelease(string Tag, string Name, string Notes, Uri Page, Uri? Installer, long? InstallerBytes);

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
        Uri? installer = null; long? installerBytes = null;
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var file = asset.TryGetProperty("name", out var fn) ? fn.GetString() : null;
                if (string.IsNullOrWhiteSpace(file) || !file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;
                var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps || !parsed.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) continue;
                installer = parsed;
                installerBytes = asset.TryGetProperty("size", out var size) && size.TryGetInt64(out var bytes) ? bytes : null;
                break;
            }
        }
        return new GitHubRelease(tag, name, notes, page, installer, installerBytes);
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