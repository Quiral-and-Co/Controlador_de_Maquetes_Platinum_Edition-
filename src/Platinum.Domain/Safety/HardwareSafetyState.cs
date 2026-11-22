namespace Platinum.Domain.Safety;

/// <summary>
/// Evidència runtime pròpia de Platinum. No depèn de JMRI; tots els valors
/// comencen en estat segur/desconnectat i cap declaració del projecte els activa.
/// </summary>
public sealed record HardwareSafetyState(
    bool CommandStationConnected = false,
    bool FeedbackHealthy = false,
    bool EmergencyStopCircuitVerified = false,
    bool WatchdogHealthy = false,
    bool InterlocksHealthy = false,
    bool OperatorSafetyAcknowledged = false,
    bool PhysicalControlArmed = false)
{
    public bool CanIssuePhysicalCommands =>
        CommandStationConnected &&
        FeedbackHealthy &&
        EmergencyStopCircuitVerified &&
        WatchdogHealthy &&
        InterlocksHealthy &&
        OperatorSafetyAcknowledged &&
        PhysicalControlArmed;
}
