using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Platinum.Domain.Control;

namespace Platinum.Infrastructure.Control;

/// <summary>
/// Central virtual local de Platinum. No obre ports ni envia ordres a la maqueta.
/// Serveix per provar la lògica de l'aplicació sense risc físic.
/// </summary>
public sealed class LocalSimulationAdapter : IControlStationAdapter
{
    private readonly Dictionary<int, int> _locomotiveSpeeds = new();
    private readonly Dictionary<int, AccessoryState> _accessories = new();
    private readonly Dictionary<(int Address, int Function), bool> _functions = new();

    public LocalSimulationAdapter(string name = "Platinum · Central virtual")
    {
        Profile = new ControlStationProfile(name, ControlStationKind.Simulation, "localhost", 0, true);
    }

    public ControlStationProfile Profile { get; }
    public bool IsConnected { get; private set; }
    public bool IsSimulation => true;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<RailwayCommandResult> ExecuteAsync(
        RailwayCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConnected)
            return Task.FromResult(RailwayCommandResult.Rejected("Central virtual desconnectada."));

        switch (command)
        {
            case LocomotiveSpeedCommand speed:
                _locomotiveSpeeds[speed.Address] = speed.Speed;
                return Task.FromResult(RailwayCommandResult.Success(
                    $"Locomotora {speed.Address}: velocitat virtual {speed.Speed}, sentit {(speed.Reverse ? "invers" : "endavant")}."));

            case LocomotiveFunctionCommand function:
                _functions[(function.Address, function.Function)] = function.Enabled;
                return Task.FromResult(RailwayCommandResult.Success(
                    $"Locomotora {function.Address}: F{function.Function} {(function.Enabled ? "ON" : "OFF")} (simulat)."));

            case AccessoryCommand accessory:
                _accessories[accessory.Address] = accessory.State;
                return Task.FromResult(RailwayCommandResult.Success(
                    $"Accessori {accessory.Address}: {accessory.State} (simulat)."));

            case EmergencyStopCommand stop:
                foreach (var address in new List<int>(_locomotiveSpeeds.Keys))
                    _locomotiveSpeeds[address] = 0;
                return Task.FromResult(RailwayCommandResult.Success(
                    $"Aturada virtual aplicada a totes les locomotores. Motiu: {stop.Reason}"));

            default:
                return Task.FromResult(RailwayCommandResult.Rejected("Tipus d'ordre no reconegut."));
        }
    }

    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        _locomotiveSpeeds.Clear();
        _accessories.Clear();
        _functions.Clear();
        return ValueTask.CompletedTask;
    }
}
