using System;
using Platinum.Domain.Control;

namespace Platinum.Infrastructure.Control;

/// <summary>
/// Crea l'adaptador propi segons el perfil. Només la simulació està disponible
/// fins que els protocols físics tinguin implementació i proves verificables.
/// </summary>
public static class ControlStationAdapterFactory
{
    public static IControlStationAdapter Create(ControlStationProfile profile)
    {
        if (profile is null)
            throw new ArgumentNullException(nameof(profile));

        if (profile.Kind == ControlStationKind.Simulation || profile.IsSimulation)
            return new LocalSimulationAdapter(profile.Name);

        throw new NotSupportedException(
            $"El protocol {profile.Kind} encara no està implementat a Platinum. " +
            "No s'intentarà connectar ni delegar el control a JMRI.");
    }
}
