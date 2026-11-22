using System;
using System.Threading;
using System.Threading.Tasks;
using Platinum.Domain.Control;

namespace Platinum.Application.Control;

/// <summary>
/// Servei d'aplicació propi per governar la maqueta.
/// La sortida física queda bloquejada explícitament en aquesta fase.
/// </summary>
public sealed class RailwayControlService
{
    private readonly IControlStationAdapter _station;

    public RailwayControlService(IControlStationAdapter station)
    {
        _station = station ?? throw new ArgumentNullException(nameof(station));
    }

    public ControlStationProfile Profile => _station.Profile;
    public bool IsConnected => _station.IsConnected;

    public Task ConnectAsync(CancellationToken cancellationToken = default) =>
        _station.ConnectAsync(cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        _station.DisconnectAsync(cancellationToken);

    public Task<RailwayCommandResult> SetLocomotiveSpeedAsync(
        int address, int speed, bool reverse, CancellationToken cancellationToken = default)
    {
        if (address is < 1 or > 10239)
            return Task.FromResult(RailwayCommandResult.Rejected("Adreça de locomotora fora de rang (1–10239)."));
        if (speed is < 0 or > 1000)
            return Task.FromResult(RailwayCommandResult.Rejected("Velocitat fora de rang (0–1000 passos interns)."));
        return ExecuteAsync(new LocomotiveSpeedCommand(address, speed, reverse), cancellationToken);
    }

    public Task<RailwayCommandResult> SetFunctionAsync(
        int address, int function, bool enabled, CancellationToken cancellationToken = default)
    {
        if (address is < 1 or > 10239)
            return Task.FromResult(RailwayCommandResult.Rejected("Adreça de locomotora fora de rang (1–10239)."));
        if (function is < 0 or > 68)
            return Task.FromResult(RailwayCommandResult.Rejected("Funció fora de rang (F0–F68)."));
        return ExecuteAsync(new LocomotiveFunctionCommand(address, function, enabled), cancellationToken);
    }

    public Task<RailwayCommandResult> SetAccessoryAsync(
        int address, AccessoryState state, CancellationToken cancellationToken = default)
    {
        if (address is < 1 or > 2048)
            return Task.FromResult(RailwayCommandResult.Rejected("Adreça d'accessori fora de rang (1–2048)."));
        return ExecuteAsync(new AccessoryCommand(address, state), cancellationToken);
    }

    public Task<RailwayCommandResult> EmergencyStopAsync(
        string reason = "Aturada sol·licitada per l'operador",
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(new EmergencyStopCommand(reason), cancellationToken);

    private Task<RailwayCommandResult> ExecuteAsync(
        RailwayCommand command, CancellationToken cancellationToken)
    {
        if (!_station.IsConnected)
            return Task.FromResult(RailwayCommandResult.Rejected("La central no està connectada."));

        // Només s'accepta l'adaptador de simulació. Els adaptadors físics resten
        // bloquejats fins a disposar de protocol, interbloqueigs i proves de maquinari.
        if (!_station.IsSimulation)
            return Task.FromResult(RailwayCommandResult.Rejected(
                "Control físic bloquejat: adaptador no habilitat per a operació real."));

        return _station.ExecuteAsync(command, cancellationToken);
    }
}
