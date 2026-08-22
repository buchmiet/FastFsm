using FastFsm.Observability;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Observability;

public sealed class ObservabilityLoggingCoexistenceTests
{
    [Fact]
    public void Generated_logger_constructor_and_observability_extension_can_coexist()
    {
        using var harness = new ObservabilityTestHarness();
        var logger = new Mock<ILogger<CoexistenceMachine>>().Object;
        var options = new FastFsmObservabilityOptions
        {
            Logging = false,
            Tracing = false,
            EventStream = true
        };
        var observability = new ObservabilityExtension<CoexistenceState, CoexistenceTrigger>(
            options,
            eventSink: harness.EventSink);
        var machine = new CoexistenceMachine(CoexistenceState.A, [observability], logger);

        machine.Start();
        Assert.True(machine.TryFire(CoexistenceTrigger.Go));

        Assert.Equal(CoexistenceState.B, machine.CurrentState);
        Assert.Contains(
            harness.EventSink.Events,
            evt => evt.Kind == ObservabilityEventKind.AttemptCompleted
                && evt.Outcome == nameof(FastFsm.Contracts.TransitionOutcome.Succeeded));
    }
}
