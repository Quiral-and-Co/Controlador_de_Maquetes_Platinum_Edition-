namespace Platinum.Domain.Safety;

/// <summary>Runtime evidence only; all values default to the safe, disconnected state.</summary>
public sealed record HardwareSafetyState(
    bool JmriConnected = false,
    bool CommandStationConnected = false,
    bool FeedbackHealthy = false,
    bool EmergencyStopCircuitVerified = false,
    bool OperatorSafetyAcknowledged = false,
    bool PhysicalControlArmed = false)
{
    public bool CanIssuePhysicalCommands =>
        JmriConnected &&
        CommandStationConnected &&
        FeedbackHealthy &&
        EmergencyStopCircuitVerified &&
        OperatorSafetyAcknowledged &&
        PhysicalControlArmed;
}
