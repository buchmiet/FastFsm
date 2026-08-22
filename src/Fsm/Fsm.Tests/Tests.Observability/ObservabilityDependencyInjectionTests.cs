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
    public void AddFastFsmObservability_registers_extension_with_requested_hook_mask()
    {
        _services.AddLogging();
        _services.AddFastFsmObservability<DiObservabilityState, DiObservabilityTrigger>(options =>
        {
            options.EventStream = true;
            options.Metrics = true;
        });
        _provider = _services.BuildServiceProvider();

        var extension = _provider.GetRequiredService<IStateMachineExtension<DiObservabilityState, DiObservabilityTrigger>>();
        var observability = Assert.IsType<ObservabilityExtension<DiObservabilityState, DiObservabilityTrigger>>(extension);
        Assert.True(observability.Hooks.HasFlag(ExtensionHooks.Transitions));
        Assert.True(observability.Hooks.HasFlag(ExtensionHooks.Lifecycle));
    }

    [Fact]
    public void AddFastFsmObservability_second_state_pair_gets_its_own_options_snapshot()
    {
        _services.AddFastFsmObservability<DiObservabilityState, DiObservabilityTrigger>(options =>
            options.Metrics = true);
        _services.AddStateMachineObservabilityExtension<ObservabilityAltState, ObservabilityAltTrigger>(
            new FastFsmObservabilityOptions { Tracing = true });

        _provider = _services.BuildServiceProvider();

        var first = _provider.GetRequiredService<ObservabilityExtension<DiObservabilityState, DiObservabilityTrigger>>();
        var second = _provider.GetRequiredService<ObservabilityExtension<ObservabilityAltState, ObservabilityAltTrigger>>();

        Assert.Equal(ExtensionHooks.Transitions, first.Hooks);
        Assert.Equal(ExtensionHooks.Transitions, second.Hooks);
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

public enum ObservabilityAltState { A, B }
public enum ObservabilityAltTrigger { Go }
