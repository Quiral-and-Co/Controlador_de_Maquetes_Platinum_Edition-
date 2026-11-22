using System;
using System.Collections.Generic;
using System.Linq;

namespace Platinum.Application.Control;

/// <summary>
/// Motor d'automatització propi de Platinum. Substitueix les responsabilitats
/// de blocs, enclavaments, itineraris i aspectes de senyal de JMRI.
/// No comunica directament amb maquinari: treballa sobre estats lògics.
/// </summary>
public sealed class RailwayAutomationEngine
{
    private readonly object _sync = new();
    private readonly Dictionary<string, BlockState> _blocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RouteState> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SignalAspect> _signals = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<AutomationEvent> _events = new();
    private bool _emergencyLatched;

    public bool EmergencyLatched { get { lock (_sync) return _emergencyLatched; } }

    public IReadOnlyList<BlockSnapshot> Blocks
    {
        get { lock (_sync) return _blocks.Values.Select(x => new BlockSnapshot(x.Name, x.Occupied, x.ReservedBy)).ToArray(); }
    }

    public IReadOnlyList<RouteSnapshot> Routes
    {
        get { lock (_sync) return _routes.Values.Select(x => new RouteSnapshot(x.Name, x.Blocks.ToArray(), x.Active)).ToArray(); }
    }

    public IReadOnlyList<AutomationEvent> Events
    {
        get { lock (_sync) return _events.ToArray(); }
    }

    public void AddBlock(string name)
    {
        name = Required(name, nameof(name));
        lock (_sync)
        {
            if (!_blocks.TryAdd(name, new BlockState(name)))
                throw new InvalidOperationException($"El bloc '{name}' ja existeix.");
            Record("BLOCK_CREATED", $"Bloc {name} creat.");
        }
    }

    public void SetOccupancy(string name, bool occupied)
    {
        lock (_sync)
        {
            var block = GetBlock(name);
            if (occupied && block.ReservedBy is not null)
                throw new InvalidOperationException($"El bloc està reservat per l'itinerari {block.ReservedBy}.");
            block.Occupied = occupied;
            Record("OCCUPANCY_CHANGED", $"Bloc {block.Name}: {(occupied ? "OCUPAT" : "LLIURE")}.");
        }
    }

    /// <summary>Reserva tots els blocs de manera atòmica o no en reserva cap.</summary>
    public void ReserveRoute(string routeName, IEnumerable<string> blockNames)
    {
        routeName = Required(routeName, nameof(routeName));
        var names = blockNames.Select(RequiredBlockName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (names.Length == 0) throw new ArgumentException("L'itinerari ha de contenir blocs.", nameof(blockNames));

        lock (_sync)
        {
            if (_emergencyLatched) throw new InvalidOperationException("Aturada d'emergència enclavada: no es poden establir itineraris.");
            if (_routes.TryGetValue(routeName, out var existing) && existing.Active)
                throw new InvalidOperationException("L'itinerari ja està actiu.");

            var selected = names.Select(GetBlock).ToArray();
            var occupied = selected.Where(x => x.Occupied).Select(x => x.Name).ToArray();
            if (occupied.Length > 0) throw new InvalidOperationException("Blocs ocupats: " + string.Join(", ", occupied));
            var reserved = selected.Where(x => x.ReservedBy is not null && !x.ReservedBy.Equals(routeName, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (reserved.Length > 0) throw new InvalidOperationException("Blocs ja reservats: " + string.Join(", ", reserved.Select(x => x.Name)));

            foreach (var block in selected) block.ReservedBy = routeName;
            _routes[routeName] = new RouteState(routeName, names, true);
            Record("ROUTE_RESERVED", $"Itinerari {routeName} reservat: {string.Join(" → ", names)}.");
        }
    }

    public void ReleaseRoute(string routeName)
    {
        lock (_sync)
        {
            if (!_routes.TryGetValue(routeName, out var route) || !route.Active)
                throw new InvalidOperationException("L'itinerari no està actiu.");
            foreach (var name in route.Blocks)
            {
                var block = GetBlock(name);
                if (block.ReservedBy == route.Name) block.ReservedBy = null;
            }
            route.Active = false;
            Record("ROUTE_RELEASED", $"Itinerari {route.Name} alliberat.");
        }
    }

    public SignalAspect GetSignalAspect(string signalName)
    {
        lock (_sync) return _signals.TryGetValue(signalName, out var aspect) ? aspect : SignalAspect.Stop;
    }

    /// <summary>Aspecte calculat de manera conservadora: parada per defecte.</summary>
    public SignalAspect EvaluateSignal(string signalName, string protectedBlock)
    {
        lock (_sync)
        {
            var block = GetBlock(protectedBlock);
            var aspect = !_emergencyLatched && !block.Occupied && block.ReservedBy is not null
                ? SignalAspect.Clear
                : SignalAspect.Stop;
            _signals[Required(signalName, nameof(signalName))] = aspect;
            Record("SIGNAL_EVALUATED", $"Senyal {signalName}: {aspect} (bloc {block.Name}).");
            return aspect;
        }
    }

    public void TriggerEmergencyStop(string reason)
    {
        lock (_sync)
        {
            _emergencyLatched = true;
            foreach (var signal in _signals.Keys.ToArray()) _signals[signal] = SignalAspect.Stop;
            Record("EMERGENCY_LATCHED", "ATURADA ENCLAVADA: " + (string.IsNullOrWhiteSpace(reason) ? "motiu no indicat" : reason));
        }
    }

    /// <summary>
    /// Restabliment lògic explícit. No equival a rearmar cap sortida física.
    /// Requereix que l'operador declari inspeccionada la causa.
    /// </summary>
    public void AcknowledgeEmergencyReset(bool causeInspected)
    {
        lock (_sync)
        {
            if (!_emergencyLatched) return;
            if (!causeInspected) throw new InvalidOperationException("Cal inspeccionar i reconèixer la causa abans del restabliment.");
            _emergencyLatched = false;
            foreach (var signal in _signals.Keys.ToArray()) _signals[signal] = SignalAspect.Stop;
            Record("EMERGENCY_RESET_ACK", "Enclavament lògic restablert; senyals mantinguts a parada.");
        }
    }

    private BlockState GetBlock(string name) =>
        _blocks.TryGetValue(name, out var block) ? block : throw new KeyNotFoundException($"No existeix el bloc '{name}'.");

    private void Record(string code, string message)
    {
        _events.Enqueue(new AutomationEvent(DateTimeOffset.UtcNow, code, message));
        while (_events.Count > 500) _events.Dequeue();
    }

    private static string RequiredBlockName(string value) => Required(value, "blockNames");
    private static string Required(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("El valor no pot estar buit.", parameter);
        return value.Trim();
    }

    private sealed class BlockState(string name)
    {
        public string Name { get; } = name;
        public bool Occupied { get; set; }
        public string? ReservedBy { get; set; }
    }

    private sealed class RouteState(string name, string[] blocks, bool active)
    {
        public string Name { get; } = name;
        public string[] Blocks { get; } = blocks;
        public bool Active { get; set; } = active;
    }
}

public enum SignalAspect { Stop, Clear }
public sealed record BlockSnapshot(string Name, bool Occupied, string? ReservedBy);
public sealed record RouteSnapshot(string Name, IReadOnlyList<string> Blocks, bool Active);
public sealed record AutomationEvent(DateTimeOffset Timestamp, string Code, string Message);
