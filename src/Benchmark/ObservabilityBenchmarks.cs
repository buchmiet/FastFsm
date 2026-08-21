using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Abstractions.Attributes;
using BenchmarkDotNet.Attributes;
using FastFsm.Contracts;
using FastFsm.Observability;

namespace Benchmark;

public enum ObsBenchState { A, B, Parent, Child1, Child2 }
public enum ObsBenchTrigger { Next, Reject, Enter, Toggle }

[StateMachine(typeof(ObsBenchState), typeof(ObsBenchTrigger), GenerateExtensibleVersion = true)]
public partial class ObsFlatExtensibleMachine
{
    [Transition(ObsBenchState.A, ObsBenchTrigger.Next, ObsBenchState.B)]
    [Transition(ObsBenchState.B, ObsBenchTrigger.Next, ObsBenchState.A)]
    [Transition(ObsBenchState.A, ObsBenchTrigger.Reject, ObsBenchState.B, Guard = nameof(Reject))]
    private void Configure() { }

    private bool Reject() => false;
}

[StateMachine(
    typeof(ObsBenchState),
    typeof(ObsBenchTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class ObsHsmExtensibleMachine
{
    [State(ObsBenchState.Parent)]
    [State(ObsBenchState.Child1, Parent = ObsBenchState.Parent, IsInitial = true)]
    [State(ObsBenchState.Child2, Parent = ObsBenchState.Parent)]
    private void ConfigureStates() { }

    [Transition(ObsBenchState.A, ObsBenchTrigger.Enter, ObsBenchState.Parent)]
    [Transition(ObsBenchState.Child1, ObsBenchTrigger.Toggle, ObsBenchState.Child2)]
    [Transition(ObsBenchState.Child2, ObsBenchTrigger.Toggle, ObsBenchState.Child1)]
    private void ConfigureTransitions() { }
}

[InProcess]
[WarmupCount(3)]
[IterationCount(15)]
[MemoryDiagnoser]
[BenchmarkCategory("Observability", "Flat")]
public class FlatObservabilityBenchmarks
{
    private const int Operations = 512;
    private ObsFlatExtensibleMachine _baseline = null!;
    private ObsFlatExtensibleMachine _allDisabled = null!;
    private ObsFlatExtensibleMachine _metricsOnly = null!;
    private ObsFlatExtensibleMachine _tracingNoListener = null!;
    private ObsFlatExtensibleMachine _tracingWithListener = null!;
    private ObsFlatExtensibleMachine _tracingAndMetrics = null!;

    [GlobalSetup]
    public void Setup()
    {
        _baseline = new ObsFlatExtensibleMachine(ObsBenchState.A, null);
        _allDisabled = Create(new FastFsmObservabilityOptions());
        _metricsOnly = Create(new FastFsmObservabilityOptions { Metrics = true });
        _tracingNoListener = Create(new FastFsmObservabilityOptions { Tracing = true });
        _tracingWithListener = Create(new FastFsmObservabilityOptions { Tracing = true });
        _tracingAndMetrics = Create(new FastFsmObservabilityOptions { Tracing = true, Metrics = true });

        foreach (var machine in new[]
                 {
                     _baseline, _allDisabled, _metricsOnly, _tracingNoListener, _tracingWithListener, _tracingAndMetrics
                 })
        {
            machine.Start();
        }
    }

    private static ObsFlatExtensibleMachine Create(FastFsmObservabilityOptions options)
    {
        var extension = new ObservabilityExtension<ObsBenchState, ObsBenchTrigger>(options);
        return new ObsFlatExtensibleMachine(ObsBenchState.A, [extension]);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public void BaselineWithoutObservability()
    {
        for (var i = 0; i < Operations; i++)
            _baseline.TryFire(ObsBenchTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void ObservabilityRegisteredAllDisabled()
    {
        for (var i = 0; i < Operations; i++)
            _allDisabled.TryFire(ObsBenchTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void MetricsOnly()
    {
        for (var i = 0; i < Operations; i++)
            _metricsOnly.TryFire(ObsBenchTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void TracingWithoutActivityListener()
    {
        for (var i = 0; i < Operations; i++)
            _tracingNoListener.TryFire(ObsBenchTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void TracingWithActivityListener()
    {
        using (ObservabilityBenchmarkActivityListener.Activate())
        {
            for (var i = 0; i < Operations; i++)
                _tracingWithListener.TryFire(ObsBenchTrigger.Next);
        }
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void TracingAndMetrics()
    {
        for (var i = 0; i < Operations; i++)
            _tracingAndMetrics.TryFire(ObsBenchTrigger.Next);
    }
}

[InProcess]
[WarmupCount(3)]
[IterationCount(15)]
[MemoryDiagnoser]
[BenchmarkCategory("Observability", "HSM")]
public class HsmObservabilityBenchmarks
{
    private const int Operations = 256;
    private ObsHsmExtensibleMachine _baseline = null!;
    private ObsHsmExtensibleMachine _metricsOnly = null!;

    [GlobalSetup]
    public void Setup()
    {
        _baseline = new ObsHsmExtensibleMachine(ObsBenchState.A, null);
        _metricsOnly = new ObsHsmExtensibleMachine(
            ObsBenchState.A,
            [new ObservabilityExtension<ObsBenchState, ObsBenchTrigger>(new FastFsmObservabilityOptions { Metrics = true })]);

        _baseline.Start();
        _baseline.TryFire(ObsBenchTrigger.Enter);
        _metricsOnly.Start();
        _metricsOnly.TryFire(ObsBenchTrigger.Enter);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public void BaselineWithoutObservability()
    {
        for (var i = 0; i < Operations; i++)
            _baseline.TryFire(ObsBenchTrigger.Toggle);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void MetricsOnly()
    {
        for (var i = 0; i < Operations; i++)
            _metricsOnly.TryFire(ObsBenchTrigger.Toggle);
    }
}
