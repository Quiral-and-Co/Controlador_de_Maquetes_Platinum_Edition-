using System.Net;
using System.Net.Http;
using System.Text.Json;
using Platinum.Domain.Control;

namespace Platinum.Infrastructure.Control;

/// <summary>Adaptador natiu de diagnòstic JMRI JSON HTTP. No delega ordres a cap procés Java local.</summary>
public sealed class JmriJsonAdapter : IControlStationAdapter
{
    private readonly HttpClient _http;
    private readonly TimeSpan _timeout;
    public JmriJsonAdapter(ControlStationProfile profile, TimeSpan? timeout = null, HttpMessageHandler? handler = null)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (profile.Kind != ControlStationKind.JmriJson) throw new ArgumentException("Perfil JMRI JSON requerit.", nameof(profile));
        if (!Uri.TryCreate($"http://{profile.Host}:{(profile.Port == 0 ? 12080 : profile.Port)}/json/", UriKind.Absolute, out var uri))
            throw new ArgumentException("Adreça JMRI no vàlida.", nameof(profile));
        BaseUri = uri;
        _timeout = timeout ?? TimeSpan.FromSeconds(3);
        if (_timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _http = new HttpClient(handler ?? new HttpClientHandler(), disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }
    public ControlStationProfile Profile { get; }
    public Uri BaseUri { get; }
    public bool IsConnected { get; private set; }
    public bool IsSimulation => false;
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);
        try
        {
            using var response = await _http.GetAsync(BaseUri, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
            if (json.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                throw new InvalidDataException("Resposta JMRI JSON invàlida: s'esperava un objecte o una llista.");
            IsConnected = true;
        }
        catch { IsConnected = false; throw; }
    }
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); IsConnected = false; return Task.CompletedTask; }
    public Task<RailwayCommandResult> ExecuteAsync(RailwayCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(RailwayCommandResult.Rejected(IsConnected
            ? "Ordre física JMRI bloquejada: falta el gate de seguretat Platinum i la verificació de maquinari."
            : "JMRI no està connectat."));
    }
    public void Dispose() { _http.Dispose(); IsConnected = false; }
    public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
}
