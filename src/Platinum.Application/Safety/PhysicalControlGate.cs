using Platinum.Domain.Safety;

namespace Platinum.Application.Safety;

/// <summary>Central policy gate. Physical commands are denied unless every runtime condition is true.</summary>
public sealed class PhysicalControlGate
{
    public bool CanIssueCommands(HardwareSafetyState state) =>
        state is not null && state.CanIssuePhysicalCommands;

    public void EnsureCommandsAllowed(HardwareSafetyState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!CanIssueCommands(state))
            throw new InvalidOperationException("Ordres físics bloquejades: manca una o més comprovacions de seguretat en temps real.");
    }
}
