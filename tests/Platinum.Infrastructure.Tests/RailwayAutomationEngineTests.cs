using System;
using System.Linq;
using Platinum.Application.Control;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class RailwayAutomationEngineTests
{
    [Fact]
    public void RouteReservation_IsAtomic_WhenAnyBlockIsOccupied()
    {
        var engine = new RailwayAutomationEngine();
        engine.AddBlock("B1");
        engine.AddBlock("B2");
        engine.SetOccupancy("B2", true);

        Assert.Throws<InvalidOperationException>(() => engine.ReserveRoute("R1", new[] { "B1", "B2" }));
        Assert.Null(engine.Blocks.Single(x => x.Name == "B1").ReservedBy);
    }

    [Fact]
    public void TwoRoutes_CannotReserveTheSameBlock()
    {
        var engine = new RailwayAutomationEngine();
        engine.AddBlock("B1");
        engine.ReserveRoute("R1", new[] { "B1" });

        Assert.Throws<InvalidOperationException>(() => engine.ReserveRoute("R2", new[] { "B1" }));
    }

    [Fact]
    public void EmergencyStop_LatchesAndPreventsNewRoutes()
    {
        var engine = new RailwayAutomationEngine();
        engine.AddBlock("B1");
        engine.TriggerEmergencyStop("test");

        Assert.Throws<InvalidOperationException>(() => engine.ReserveRoute("R1", new[] { "B1" }));
        Assert.Throws<InvalidOperationException>(() => engine.AcknowledgeEmergencyReset(false));
        engine.AcknowledgeEmergencyReset(true);
        engine.ReserveRoute("R1", new[] { "B1" });
    }

    [Fact]
    public void SignalDefaultsToStop_AndClearsOnlyForReservedFreeBlock()
    {
        var engine = new RailwayAutomationEngine();
        engine.AddBlock("B1");
        Assert.Equal(SignalAspect.Stop, engine.GetSignalAspect("S1"));
        Assert.Equal(SignalAspect.Stop, engine.EvaluateSignal("S1", "B1"));

        engine.ReserveRoute("R1", new[] { "B1" });
        Assert.Equal(SignalAspect.Clear, engine.EvaluateSignal("S1", "B1"));
    }
}
