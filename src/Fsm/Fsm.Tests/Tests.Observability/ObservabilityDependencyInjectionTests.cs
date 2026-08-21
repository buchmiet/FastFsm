using FastFsm.Contracts;
using FastFsm.Observability;
using FastFsm.Observability.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Observability;

public sealed class ObservabilityDependencyInjectionTests : IDisposable
{
    private readonly ServiceCollection _services = new();
    private ServiceProvider? _provider;

    [Fact]
    public void AddFastFsmObservability_registers_observability_extension_and_options()
    {
        _services.AddLogging();
        _services.AddFastFsmObservability<DiObservabilityState, DiObservabilityTrigger>(options =>
        {
            options.EventStream = true;
            options.Metrics = true;
        });
        _provider = _services.BuildServiceProvider();

        var extension = _provider.GetRequiredService<IStateMachineExtension<DiObservabilityState, DiObservabilityTrigger>>();
        Assert.IsType<ObservabilityExtension<DiObservabilityState, DiObservabilityTrigger>>(extension);

        var options = _provider.GetRequiredService<FastFsmObservabilityOptions>();
        Assert.True(options.EventStream);
        Assert.True(options.Metrics);
    }

    [Fact]
    public void AddFastFsmObservability_extension_can_be_attached_to_machine()
    {
        using var harness = new ObservabilityTestHarness();
        _services.AddSingleton<IObservabilityEventSink>(harness.EventSink);
        _services.AddFastFsmObservability<DiObservabilityState, DiObservabilityTrigger>(options =>
            options.EventStream = true);
        _provider = _services.BuildServiceProvider();

        var extension = _provider.GetRequiredService<IStateMachineExtension<DiObservabilityState, DiObservabilityTrigger>>();
        var machine = new DiObservabilityMachine(DiObservabilityState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(DiObservabilityTrigger.Go));
        Assert.Equal(DiObservabilityState.B, machine.CurrentState);
        Assert.Contains(
            harness.EventSink.Events,
            evt => evt.Kind == ObservabilityEventKind.AttemptCompleted);
    }

    public void Dispose() => _provider?.Dispose();
}
