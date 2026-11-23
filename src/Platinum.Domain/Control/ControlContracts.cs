using System;
using System.Threading;
using System.Threading.Tasks;

namespace Platinum.Domain.Control;

public enum ControlStationKind
{
    Simulation,
    MarklinCs2,
    MarklinCs3,
    Z21,
    EsuEcos,
    GenericDcc

}

public enum AccessoryState
{
    Straight,
    Diverging,
    On,
    Off
}

public abstract record RailwayCommand;

public sealed record LocomotiveSpeedCommand(int Address, int Speed, bool Reverse) : RailwayCommand;
public sealed record LocomotiveFunctionCommand(int Address, int Function, bool Enabled) : RailwayCommand;
public sealed record AccessoryCommand(int Address, AccessoryState State) : RailwayCommand;
public sealed record EmergencyStopCommand(string Reason) : RailwayCommand;

public sealed record ControlStationProfile(
    string Name,
    ControlStationKind Kind,
    string Host,
    int Port,
    bool IsSimulation = false);

public sealed record RailwayCommandResult(bool Accepted, string Message)
{
    public static RailwayCommandResult Success(string message) => new(true, message);
    public static RailwayCommandResult Rejected(string message) => new(false, message);
}

/// <summary>
/// Contracte propi de Platinum. Cap dependència de JMRI ni d'un servidor extern.
/// Els adaptadors físics s'han d'implementar i validar per a cada central.
/// </summary>
public interface IControlStationAdapter : IAsyncDisposable
{
    ControlStationProfile Profile { get; }
    bool IsConnected { get; }
    bool IsSimulation { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<RailwayCommandResult> ExecuteAsync(
        RailwayCommand command,
        CancellationToken cancellationToken = default);
}
