using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Platinum.Domain.Control;

namespace Platinum.Infrastructure.Control;

/// <summary>
/// Transport natiu Märklin CS2 CAN-over-Ethernet (datagrames de 13 bytes).
/// No utilitza JMRI, Java, WebSocket ni servidor intermediari.
/// Les ordres de tracció/accessoris continuen bloquejades fins a integrar
/// l'estat de seguretat runtime i validar la instal·lació física.
/// </summary>
public sealed class MarklinCs2Adapter : IControlStationAdapter
{
    public const int DefaultCommandPort = 15731;
    public const int DefaultFeedbackPort = 15730;
    private const int FrameLength = 13;
    private const int PingCommand = 0x18;
    private const ushort ProtocolHash = 0x4711; // Fingerprint CS2: bits 7..9 = 110.

    private readonly IPAddress _address;
    private readonly int _commandPort;
    private readonly int _feedbackPort;
    private readonly TimeSpan _timeout;
    private UdpClient? _udp;
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public MarklinCs2Adapter(ControlStationProfile profile, TimeSpan? timeout = null)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (profile.Kind != ControlStationKind.MarklinCs2)
            throw new ArgumentException("El perfil ha de ser Märklin CS2.", nameof(profile));
        if (!IPAddress.TryParse(profile.Host, out _address!))
            throw new ArgumentException("Indica la IP numèrica de la CS2 (no s'accepten noms DNS en aquesta fase).", nameof(profile));
        _commandPort = profile.Port == 0 ? DefaultCommandPort : profile.Port;
        if (_commandPort is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(profile), "Port CS2 no vàlid.");
        _feedbackPort = DefaultFeedbackPort;
        _timeout = timeout ?? TimeSpan.FromSeconds(3);
        if (_timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
    }

    public ControlStationProfile Profile { get; }
    public bool IsConnected { get; private set; }
    public bool IsSimulation => false;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        var client = new UdpClient(new IPEndPoint(IPAddress.Any, _feedbackPort));
        client.EnableBroadcast = true;
        _udp = client;
        try
        {
            // Només consulta de presència: no activa la via ni envia ordres de moviment.
            var request = MarklinCs2FrameCodec.Create(PingCommand, priority: 0, response: false, ProtocolHash, Array.Empty<byte>());
            await client.SendAsync(request, new IPEndPoint(_address, _commandPort), cancellationToken).ConfigureAwait(false);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);
            while (true)
            {
                var received = await client.ReceiveAsync(timeout.Token).ConfigureAwait(false);
                if (!received.RemoteEndPoint.Address.Equals(_address)) continue;
                if (!MarklinCs2FrameCodec.TryParse(received.Buffer, out var frame)) continue;
                if (frame.Command == PingCommand && frame.Response && frame.DataLength == 8)
                {
                    IsConnected = true;
                    return;
                }
            }
        }
        catch
        {
            client.Dispose();
            _udp = null;
            IsConnected = false;
            throw;
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsConnected = false;
        _udp?.Dispose();
        _udp = null;
        return Task.CompletedTask;
    }

    public Task<RailwayCommandResult> ExecuteAsync(
        RailwayCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
            return Task.FromResult(RailwayCommandResult.Rejected("Märklin CS2 no està connectada."));
        return Task.FromResult(RailwayCommandResult.Rejected(
            "Ordre física bloquejada a Platinum: manca la validació de seguretat en temps real, el mapa de Loc-ID/UID verificat i les proves amb maquinari."));
    }

    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        _udp?.Dispose();
        _udp = null;
        _ioLock.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Codificador/descodificador pur del paquet Ethernet CS2 CAN 2.0 (13 bytes).</summary>
public static class MarklinCs2FrameCodec
{
    public const int FrameLength = 13;

    public static byte[] Create(int command, int priority, bool response, ushort hash, ReadOnlySpan<byte> data)
    {
        if (command is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(command));
        if (priority is < 0 or > 15) throw new ArgumentOutOfRangeException(nameof(priority));
        if (data.Length > 8) throw new ArgumentOutOfRangeException(nameof(data));
        if ((hash & 0x0380) != 0x0300) throw new ArgumentException("Hash sense fingerprint CS2 vàlid.", nameof(hash));

        var identifier = ((uint)priority << 25) |
                         ((uint)command << 17) |
                         (response ? 1u << 16 : 0u) |
                         hash;
        var packet = new byte[FrameLength];
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(0, 4), identifier);
        packet[4] = (byte)data.Length;
        data.CopyTo(packet.AsSpan(5, 8));
        return packet;
    }

    public static bool TryParse(ReadOnlySpan<byte> packet, out MarklinCs2Frame frame)
    {
        frame = default;
        if (packet.Length != FrameLength) return false;
        var identifier = BinaryPrimitives.ReadUInt32BigEndian(packet[..4]);
        var priority = (int)((identifier >> 25) & 0x0F);
        var command = (int)((identifier >> 17) & 0xFF);
        var response = ((identifier >> 16) & 1) != 0;
        var hash = (ushort)(identifier & 0xFFFF);
        var dlc = packet[4];
        if (dlc > 8 || (hash & 0x0380) != 0x0300) return false;
        frame = new MarklinCs2Frame(priority, command, response, hash, dlc, packet.Slice(5, 8).ToArray());
        return true;
    }
}

public readonly record struct MarklinCs2Frame(
    int Priority, int Command, bool Response, ushort Hash, int DataLength, byte[] Data);
