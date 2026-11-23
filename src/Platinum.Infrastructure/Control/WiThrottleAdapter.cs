using System.Net.Sockets;
using System.Text;
using Platinum.Domain.Control;

namespace Platinum.Infrastructure.Control;

/// <summary>Handshake TCP natiu WiThrottle. La sessió s'utilitza per diagnòstic; ordres físiques bloquejades.</summary>
public sealed class WiThrottleAdapter : IControlStationAdapter
{
    private readonly TimeSpan _timeout;
    private readonly string _clientName;
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    public WiThrottleAdapter(ControlStationProfile profile, TimeSpan? timeout = null, string clientName = "Platinum")
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (profile.Kind != ControlStationKind.WiThrottle) throw new ArgumentException("Perfil WiThrottle requerit.", nameof(profile));
        if (string.IsNullOrWhiteSpace(profile.Host)) throw new ArgumentException("Host WiThrottle obligatori.", nameof(profile));
        if (profile.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(profile));
        _timeout = timeout ?? TimeSpan.FromSeconds(3);
        if (_timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _clientName = string.IsNullOrWhiteSpace(clientName) ? "Platinum" : clientName.Replace("\r","").Replace("\n","");
    }
    public ControlStationProfile Profile { get; }
    public bool IsConnected { get; private set; }
    public bool IsSimulation => false;
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        var client = new TcpClient();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);
            await client.ConnectAsync(Profile.Host, Profile.Port, cts.Token).ConfigureAwait(false);
            var stream = client.GetStream();
            var reader = new StreamReader(stream, new UTF8Encoding(false), false, 1024, leaveOpen: true);
            var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true) { NewLine = "\n", AutoFlush = true };
            await writer.WriteLineAsync("N" + _clientName).WaitAsync(cts.Token).ConfigureAwait(false);
            var hello = await reader.ReadLineAsync(cts.Token).ConfigureAwait(false);
            if (hello is null || !hello.StartsWith("VN", StringComparison.Ordinal))
                throw new InvalidDataException($"Handshake WiThrottle invàlid: {hello ?? "connexió tancada"}");
            _client = client; _reader = reader; _writer = writer; IsConnected = true;
        }
        catch { client.Dispose(); IsConnected = false; throw; }
    }
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsConnected = false; _reader?.Dispose(); _writer?.Dispose(); _client?.Dispose();
        _reader = null; _writer = null; _client = null;
        return Task.CompletedTask;
    }
    public Task<RailwayCommandResult> ExecuteAsync(RailwayCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(RailwayCommandResult.Rejected(IsConnected
            ? "Ordre WiThrottle bloquejada: falta el gate de seguretat Platinum i la verificació de maquinari."
            : "WiThrottle no està connectat."));
    }
    public ValueTask DisposeAsync() { _ = DisconnectAsync(); return ValueTask.CompletedTask; }
}
