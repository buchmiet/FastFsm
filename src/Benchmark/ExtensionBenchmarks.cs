using System.Threading.Tasks;
using Abstractions.Attributes;
using BenchmarkDotNet.Attributes;
using FastFsm.Contracts;

namespace Benchmark;

public enum ExtensionBenchmarkState { A, B }
public enum ExtensionBenchmarkTrigger { Next, Reject, Missing, Internal }

[StateMachine(typeof(ExtensionBenchmarkState), typeof(ExtensionBenchmarkTrigger))]
public partial class NonExtensibleBenchmarkMachine
{
    [Transition(ExtensionBenchmarkState.A, ExtensionBenchmarkTrigger.Next, ExtensionBenchmarkState.B)]
    [Transition(ExtensionBenchmarkState.B, ExtensionBenchmarkTrigger.Next, ExtensionBenchmarkState.A)]
    private void Configure() { }
}

[StateMachine(typeof(ExtensionBenchmarkState), typeof(ExtensionBenchmarkTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensibleBenchmarkMachine
{
    [Transition(ExtensionBenchmarkState.A, ExtensionBenchmarkTrigger.Next, ExtensionBenchmarkState.B)]
    [Transition(ExtensionBenchmarkState.B, ExtensionBenchmarkTrigger.Next, ExtensionBenchmarkState.A)]
    [Transition(ExtensionBenchmarkState.A, ExtensionBenchmarkTrigger.Reject, ExtensionBenchmarkState.B, Guard = nameof(Reject))]
    [InternalTransition(ExtensionBenchmarkState.A, ExtensionBenchmarkTrigger.Internal, Action = nameof(NoOp))]
    private void Configure() { }

    private bool Reject() => false;
    private void NoOp() { }
}

[StateMachine(typeof(ExtensionBenchmarkState), typeof(ExtensionBenchmarkTrigger))]
public partial class NonExtensibleAsyncBenchmarkMachine
{
    [Transition(ExtensionBenchmarkState.A, ExtensionBenchmarkTrigger.Next, ExtensionBenchmarkState.B, Action = nameof(NoOpAsync))]
    [Transition(ExtensionBenchmarkState.B, ExtensionBenchmarkTrigger.Next, ExtensionBenchmarkState.A, Action = nameof(NoOpAsync))]
    private void Configure() { }

    private ValueTask NoOpAsync() => ValueTask.CompletedTask;
}

[StateMachine(typeof(ExtensionBenchmarkState), typeof(ExtensionBenchmarkTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensibleAsyncBenchmarkMachine
{
    [Transition(ExtensionBenchmarkState.A, ExtensionBenchmarkTrigger.Next, ExtensionBenchmarkState.B, Action = nameof(NoOpAsync))]
    [Transition(ExtensionBenchmarkState.B, ExtensionBenchmarkTrigger.Next, ExtensionBenchmarkState.A, Action = nameof(NoOpAsync))]
    [Transition(ExtensionBenchmarkState.A, ExtensionBenchmarkTrigger.Reject, ExtensionBenchmarkState.B, Guard = nameof(RejectAsync), Action = nameof(NoOpAsync))]
    [InternalTransition(ExtensionBenchmarkState.A, ExtensionBenchmarkTrigger.Internal, Action = nameof(NoOpAsync))]
    private void Configure() { }

    private ValueTask<bool> RejectAsync() => ValueTask.FromResult(false);
    private ValueTask NoOpAsync() => ValueTask.CompletedTask;
}

public sealed class NoOpExtension : IStateMachineExtension<ExtensionBenchmarkState, ExtensionBenchmarkTrigger>
{
    public void OnAttemptStarting(
        in TransitionAttemptContext<ExtensionBenchmarkState, ExtensionBenchmarkTrigger> attempt) { }

    public void OnAttemptCompleted(
        in TransitionAttemptContext<ExtensionBenchmarkState, ExtensionBenchmarkTrigger> attempt,
        in TransitionResult<ExtensionBenchmarkState> result) { }
}

[InProcess]
[WarmupCount(3)]
[IterationCount(15)]
[MemoryDiagnoser]
[BenchmarkCategory("Extensions")]
public class ExtensionBenchmarks
{
    private const int Operations = 1024;
    private NonExtensibleBenchmarkMachine _plain = null!;
    private ExtensibleBenchmarkMachine _withoutExtensions = null!;
    private ExtensibleBenchmarkMachine _oneExtension = null!;
    private ExtensibleBenchmarkMachine _fourExtensions = null!;
    private ExtensibleBenchmarkMachine _failurePaths = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = new NonExtensibleBenchmarkMachine(ExtensionBenchmarkState.A);
        _withoutExtensions = new ExtensibleBenchmarkMachine(ExtensionBenchmarkState.A, null);
        _oneExtension = new ExtensibleBenchmarkMachine(ExtensionBenchmarkState.A, [new NoOpExtension()]);
        _fourExtensions = new ExtensibleBenchmarkMachine(
            ExtensionBenchmarkState.A,
            [new NoOpExtension(), new NoOpExtension(), new NoOpExtension(), new NoOpExtension()]);
        _failurePaths = new ExtensibleBenchmarkMachine(ExtensionBenchmarkState.A, [new NoOpExtension()]);
        _plain.Start();
        _withoutExtensions.Start();
        _oneExtension.Start();
        _fourExtensions.Start();
        _failurePaths.Start();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public void NonExtensible()
    {
        for (var i = 0; i < Operations; i++)
            _plain.TryFire(ExtensionBenchmarkTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void ExtensibleWithoutRegisteredExtensions()
    {
        for (var i = 0; i < Operations; i++)
            _withoutExtensions.TryFire(ExtensionBenchmarkTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void ExtensibleWithOneExtension()
    {
        for (var i = 0; i < Operations; i++)
            _oneExtension.TryFire(ExtensionBenchmarkTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void ExtensibleWithFourExtensions()
    {
        for (var i = 0; i < Operations; i++)
            _fourExtensions.TryFire(ExtensionBenchmarkTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void GuardRejectedWithOneExtension()
    {
        for (var i = 0; i < Operations; i++)
            _failurePaths.TryFire(ExtensionBenchmarkTrigger.Reject);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void UnhandledWithOneExtension()
    {
        for (var i = 0; i < Operations; i++)
            _failurePaths.TryFire(ExtensionBenchmarkTrigger.Missing);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public void InternalWithOneExtension()
    {
        for (var i = 0; i < Operations; i++)
            _failurePaths.TryFire(ExtensionBenchmarkTrigger.Internal);
    }
}

[InProcess]
[WarmupCount(3)]
[IterationCount(15)]
[MemoryDiagnoser]
[BenchmarkCategory("Extensions", "Async")]
public class AsyncExtensionBenchmarks
{
    private const int Operations = 256;
    private NonExtensibleAsyncBenchmarkMachine _plain = null!;
    private ExtensibleAsyncBenchmarkMachine _withoutExtensions = null!;
    private ExtensibleAsyncBenchmarkMachine _oneExtension = null!;
    private ExtensibleAsyncBenchmarkMachine _fourExtensions = null!;
    private ExtensibleAsyncBenchmarkMachine _failurePaths = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _plain = new NonExtensibleAsyncBenchmarkMachine(ExtensionBenchmarkState.A);
        _withoutExtensions = new ExtensibleAsyncBenchmarkMachine(ExtensionBenchmarkState.A, null);
        _oneExtension = new ExtensibleAsyncBenchmarkMachine(ExtensionBenchmarkState.A, [new NoOpExtension()]);
        _fourExtensions = new ExtensibleAsyncBenchmarkMachine(
            ExtensionBenchmarkState.A,
            [new NoOpExtension(), new NoOpExtension(), new NoOpExtension(), new NoOpExtension()]);
        _failurePaths = new ExtensibleAsyncBenchmarkMachine(ExtensionBenchmarkState.A, [new NoOpExtension()]);
        await _plain.StartAsync();
        await _withoutExtensions.StartAsync();
        await _oneExtension.StartAsync();
        await _fourExtensions.StartAsync();
        await _failurePaths.StartAsync();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public async ValueTask NonExtensible()
    {
        for (var i = 0; i < Operations; i++)
            await _plain.TryFireAsync(ExtensionBenchmarkTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public async ValueTask ExtensibleWithoutRegisteredExtensions()
    {
        for (var i = 0; i < Operations; i++)
            await _withoutExtensions.TryFireAsync(ExtensionBenchmarkTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public async ValueTask ExtensibleWithOneExtension()
    {
        for (var i = 0; i < Operations; i++)
            await _oneExtension.TryFireAsync(ExtensionBenchmarkTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public async ValueTask ExtensibleWithFourExtensions()
    {
        for (var i = 0; i < Operations; i++)
            await _fourExtensions.TryFireAsync(ExtensionBenchmarkTrigger.Next);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public async ValueTask GuardRejectedWithOneExtension()
    {
        for (var i = 0; i < Operations; i++)
            await _failurePaths.TryFireAsync(ExtensionBenchmarkTrigger.Reject);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public async ValueTask UnhandledWithOneExtension()
    {
        for (var i = 0; i < Operations; i++)
            await _failurePaths.TryFireAsync(ExtensionBenchmarkTrigger.Missing);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public async ValueTask InternalWithOneExtension()
    {
        for (var i = 0; i < Operations; i++)
            await _failurePaths.TryFireAsync(ExtensionBenchmarkTrigger.Internal);
    }
}