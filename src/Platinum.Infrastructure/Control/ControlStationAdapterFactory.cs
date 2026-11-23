using System;
using Platinum.Domain.Control;

namespace Platinum.Infrastructure.Control;

/// <summary>
/// Crea l'adaptador propi segons el perfil. La CS2 permet consulta nativa de presència;
/// les ordres físiques continuen bloquejades fins a validació i proves verificables.
/// </summary>
public static class ControlStationAdapterFactory
{
    public static IControlStationAdapter Create(ControlStationProfile profile)
    {
        if (profile is null)
            throw new ArgumentNullException(nameof(profile));

        if (profile.Kind == ControlStationKind.Simulation || profile.IsSimulation)
            return new LocalSimulationAdapter(profile.Name);

        if (profile.Kind == ControlStationKind.MarklinCs2)
            return new MarklinCs2Adapter(profile);

        if (profile.Kind == ControlStationKind.JmriJson)
            return new JmriJsonAdapter(profile);

        if (profile.Kind == ControlStationKind.WiThrottle)
            return new WiThrottleAdapter(profile);

        throw new NotSupportedException(
            $"El protocol {profile.Kind} encara no està implementat a Platinum. " +
            "No s'intentarà connectar ni delegar el control a JMRI.");
    }
}
