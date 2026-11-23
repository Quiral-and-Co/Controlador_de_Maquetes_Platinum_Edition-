using System.Net.Http.Headers;
using System.Text.Json;

namespace Platinum.Presentation.Avalonia.Updates;

public sealed record GitHubReleaseInfo(string Tag,string Name,string Notes,Uri Page,IReadOnlyList<string> Assets);

public sealed class GitHubReleaseService
{
    private const string Endpoint="https://api.github.com/repos/Quiral-and-Co/Controlador_de_Maquetes_Platinum_Edition-/releases/latest";
    private static readonly HttpClient Client=CreateClient();
    private static HttpClient CreateClient()
    {
        var client=new HttpClient{Timeout=TimeSpan.FromSeconds(20)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Quiral-Platinum-Avalonia/0.1");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
    public async Task<GitHubReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken=default)
    {
        using var response=await Client.GetAsync(Endpoint,HttpCompletionOption.ResponseHeadersRead,cancellationToken);
        if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return null;
        response.EnsureSuccessStatusCode();
        await using var stream=await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json=await JsonDocument.ParseAsync(stream,cancellationToken:cancellationToken);
        var root=json.RootElement;
        var tag=root.GetProperty("tag_name").GetString()??throw new InvalidDataException("Release sense tag.");
        var name=root.TryGetProperty("name",out var n)?n.GetString()??tag:tag;
        var notes=root.TryGetProperty("body",out var b)?b.GetString()??string.Empty:string.Empty;
        var pageText=root.GetProperty("html_url").GetString()??throw new InvalidDataException("Release sense URL.");
        if(!Uri.TryCreate(pageText,UriKind.Absolute,out var page)||page.Scheme!=Uri.UriSchemeHttps||!page.Host.Equals("github.com",StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("URL de Release no oficial.");
        var assets=new List<string>();
        if(root.TryGetProperty("assets",out var array)&&array.ValueKind==JsonValueKind.Array)
            foreach(var asset in array.EnumerateArray())if(asset.TryGetProperty("name",out var a)&&a.GetString() is string file)assets.Add(file);
        return new GitHubReleaseInfo(tag,name,notes,page,assets);
    }
    public static Version ParseVersion(string tag)
    {
        var value=tag.Trim();if(value.StartsWith("v",StringComparison.OrdinalIgnoreCase))value=value[1..];
        var suffix=value.IndexOfAny(['-','+']);if(suffix>=0)value=value[..suffix];
        return Version.TryParse(value,out var version)?version:new Version(0,0);
    }
}