using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Abstractions.Attributes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Stateless;

namespace Benchmark;

// ===== 1) Wspólne enumy dla HSM =====
public enum HsmState { Outside, Parent, Parent_Child1, Parent_Child2, End }
public enum HsmTrigger { EnterParent, LeaveParent, Toggle, Tick }

// ===== 2A) FastFSM – HSM Implementations =====

[Abstractions.Attributes.StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class FastFsmHsmBasic
{
    [State(HsmState.Parent, OnEntry = nameof(OnParentEntry), OnExit = nameof(OnParentExit))]
    private void SParent() { }

    [State(HsmState.Parent_Child1, Parent = HsmState.Parent, IsInitial = true)]
    [State(HsmState.Parent_Child2, Parent = HsmState.Parent)]
    private void SChildren() { }

    // Outside -> Parent (auto-zjazd do Child1), Parent -> Outside
    [Transition(HsmState.Outside, HsmTrigger.EnterParent, HsmState.Parent)]
    [Transition(HsmState.Parent, HsmTrigger.LeaveParent, HsmState.Outside)]
    // Child1<->Child2 dla ruchu po hierarchii
    [Transition(HsmState.Parent_Child1, HsmTrigger.Toggle, HsmState.Parent_Child2)]
    [Transition(HsmState.Parent_Child2, HsmTrigger.Toggle, HsmState.Parent_Child1)]
    private void Configure() { }

    // Internal na rodzicu — działa w obu childach, bez zmiany stanu
    [InternalTransition(HsmState.Parent, HsmTrigger.Tick, Action = nameof(OnTick))]
    private void InternalOnParent() { }

    private void OnParentEntry() { /* nop */ }
    private void OnParentExit() { /* zapisy historii robi generator gdy trzeba */ }
    private void OnTick() { /* nop */ }
}

[Abstractions.Attributes.StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class FastFsmHsmAsync
{
    [State(HsmState.Parent)]
    [State(HsmState.Parent_Child1, Parent = HsmState.Parent, IsInitial = true)]
    [State(HsmState.Parent_Child2, Parent = HsmState.Parent)]
    private void States() { }

    // DWIE strony Toggle (oba z async action)
    [Transition(HsmState.Parent_Child1, HsmTrigger.Toggle, HsmState.Parent_Child2, Action = nameof(DoAsyncExit))]
    [Transition(HsmState.Parent_Child2, HsmTrigger.Toggle, HsmState.Parent_Child1, Action = nameof(DoAsyncExit))]
    // Wejście/wyjście hierarchii
    [Transition(HsmState.Outside, HsmTrigger.EnterParent, HsmState.Parent)]
    [Transition(HsmState.Parent, HsmTrigger.LeaveParent, HsmState.Outside)]
    private void Transitions() { }

    private async ValueTask DoAsyncExit()
    {
        await Task.Yield(); // real context switch
    }
}

// Shallow history na rodzicu
[Abstractions.Attributes.StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class FastFsmHsmHistoryShallow
{
    [State(HsmState.Parent, History = HistoryMode.Shallow)]
    private void Parent() { }

    [State(HsmState.Parent_Child1, Parent = HsmState.Parent, IsInitial = true)]
    [State(HsmState.Parent_Child2, Parent = HsmState.Parent)]
    private void Children() { }

    [Transition(HsmState.Outside, HsmTrigger.EnterParent, HsmState.Parent)]
    [Transition(HsmState.Parent, HsmTrigger.LeaveParent, HsmState.Outside)]
    [Transition(HsmState.Parent_Child1, HsmTrigger.Toggle, HsmState.Parent_Child2)]
    [Transition(HsmState.Parent_Child2, HsmTrigger.Toggle, HsmState.Parent_Child1)]
    private void Configure() { }
}

// ===== 2B) Stateless – HSM Implementations =====

public sealed class StatelessHsm
{
    public readonly StateMachine<HsmState, HsmTrigger> SM;

    public StatelessHsm()
    {
        SM = new StateMachine<HsmState, HsmTrigger>(HsmState.Outside);

        // Hierarchia
        SM.Configure(HsmState.Parent)
            .InitialTransition(HsmState.Parent_Child1);
        SM.Configure(HsmState.Parent_Child1)
            .SubstateOf(HsmState.Parent);
        SM.Configure(HsmState.Parent_Child2)
            .SubstateOf(HsmState.Parent);

        // Wejście/wyjście hierarchii
        SM.Configure(HsmState.Outside)
            .Permit(HsmTrigger.EnterParent, HsmState.Parent);
        SM.Configure(HsmState.Parent)
            .Permit(HsmTrigger.LeaveParent, HsmState.Outside);

        // Ruch wewnątrz hierarchii
        SM.Configure(HsmState.Parent_Child1)
            .Permit(HsmTrigger.Toggle, HsmState.Parent_Child2);
        SM.Configure(HsmState.Parent_Child2)
            .Permit(HsmTrigger.Toggle, HsmState.Parent_Child1);

        // Internal na rodzicu (obowiązuje w childach)
        SM.Configure(HsmState.Parent)
            .InternalTransition(HsmTrigger.Tick, _ => { /* nop */ });
    }
}

public sealed class StatelessHsmAsync
{
    public readonly StateMachine<HsmState, HsmTrigger> SM;

    public StatelessHsmAsync()
    {
        SM = new StateMachine<HsmState, HsmTrigger>(HsmState.Outside);

        SM.Configure(HsmState.Parent).InitialTransition(HsmState.Parent_Child1);
        SM.Configure(HsmState.Parent_Child1).SubstateOf(HsmState.Parent);
        SM.Configure(HsmState.Parent_Child2).SubstateOf(HsmState.Parent);

        SM.Configure(HsmState.Outside).Permit(HsmTrigger.EnterParent, HsmState.Parent);
        SM.Configure(HsmState.Parent).Permit(HsmTrigger.LeaveParent, HsmState.Outside);

        // DWUKIERUNKOWY Toggle, oba z OnExitAsync (real async)
        SM.Configure(HsmState.Parent_Child1)
          .Permit(HsmTrigger.Toggle, HsmState.Parent_Child2)
          .OnExitAsync(async () => await Task.Yield());

        SM.Configure(HsmState.Parent_Child2)
          .Permit(HsmTrigger.Toggle, HsmState.Parent_Child1)
          .OnExitAsync(async () => await Task.Yield());
    }
}

// ===== 3) Benchmarki HSM (BDN) =====

[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 15)]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 2)]
public class HsmBenchmarks
{
    private const int Ops = 1024;
    private static int __bh;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BH(int v) => System.Threading.Volatile.Write(ref __bh, v);

    // FastFSM
    private FastFsmHsmBasic _fastBasic = null!;
    private FastFsmHsmAsync _fastAsync = null!;
    private FastFsmHsmHistoryShallow _fastHist = null!;

    // Stateless
    private StatelessHsm _slBasic = null!;
    private StatelessHsmAsync _slAsync = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        // FastFSM
        _fastBasic = new FastFsmHsmBasic(HsmState.Outside);
        _fastBasic.Start();

        _fastAsync = new FastFsmHsmAsync(HsmState.Outside);
        await _fastAsync.StartAsync();

        _fastHist = new FastFsmHsmHistoryShallow(HsmState.Outside);
        _fastHist.Start();

        // Stateless
        _slBasic = new StatelessHsm();
        _slAsync = new StatelessHsmAsync();
    }

    // ---------- WSPÓLNE: HSM-Basic (Outside -> Parent(auto Child1) -> Outside) ----------
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("HSM-Basic")]
    public void FastFSM_Hsm_Basic_EnterLeave()
    {
        int acc = 0;
        for (int i = 0; i < Ops; i++)
        {
            _fastBasic.TryFire(HsmTrigger.EnterParent); // Outside -> Parent -> Child1 (auto)
            _fastBasic.TryFire(HsmTrigger.LeaveParent); // Parent -> Outside
            acc ^= (int)_fastBasic.CurrentState;
        }
        BH(acc);
    }

    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("HSM-Basic")]
    public void Stateless_Hsm_Basic_EnterLeave()
    {
        for (int i = 0; i < Ops; i++)
        {
            _slBasic.SM.Fire(HsmTrigger.EnterParent); // Outside -> Parent -> Child1
            _slBasic.SM.Fire(HsmTrigger.LeaveParent); // Parent -> Outside
        }
        BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_slBasic.SM);
    }

    // ---------- WSPÓLNE: HSM-Internal (Parent-level internal, brak zmiany stanu) ----------
    [GlobalSetup(Targets = new[] { nameof(FastFSM_Hsm_Internal), nameof(Stateless_Hsm_Internal) })]
    public void Setup_InternalState()
    {
        // FastFSM
        if (_fastBasic == null)
        {
            _fastBasic = new FastFsmHsmBasic(HsmState.Outside);
            _fastBasic.Start();
        }
        if (_fastBasic.CurrentState == HsmState.Outside)
            _fastBasic.TryFire(HsmTrigger.EnterParent); // -> Parent_Child1

        // Stateless
        if (_slBasic == null)
            _slBasic = new StatelessHsm();

        if (_slBasic.SM.State == HsmState.Outside)
            _slBasic.SM.Fire(HsmTrigger.EnterParent);   // -> Parent_Child1
    }

    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("HSM-Internal")]
    public void FastFSM_Hsm_Internal()
    {
        int acc = 0;
        for (int i = 0; i < Ops; i++)
        {
            _fastBasic.TryFire(HsmTrigger.Tick);
            acc ^= (int)_fastBasic.CurrentState;
        }
        BH(acc);
    }

    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("HSM-Internal")]
    public void Stateless_Hsm_Internal()
    {
        for (int i = 0; i < Ops; i++)
            _slBasic.SM.Fire(HsmTrigger.Tick);
        BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_slBasic.SM);
    }

    // ---------- WSPÓLNE: HSM-Async (real async: Task.Yield) ----------
    [GlobalSetup(Targets = new[] { nameof(FastFSM_Hsm_AsyncYield), nameof(Stateless_Hsm_AsyncYield) })]
    public async Task Setup_AsyncState()
    {
        // FastFSM
        if (_fastAsync == null)
        {
            _fastAsync = new FastFsmHsmAsync(HsmState.Outside);
            await _fastAsync.StartAsync();
        }
        if (_fastAsync.CurrentState == HsmState.Outside)
            await _fastAsync.TryFireAsync(HsmTrigger.EnterParent);

        // Stateless
        if (_slAsync == null)
            _slAsync = new StatelessHsmAsync();

        if (_slAsync.SM.State == HsmState.Outside)
            _slAsync.SM.Fire(HsmTrigger.EnterParent);
    }

    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("HSM-Async-Yield")]
    public async ValueTask FastFSM_Hsm_AsyncYield()
    {
        int acc = 0;
        for (int i = 0; i < Ops; i++)
        {
            await _fastAsync.TryFireAsync(HsmTrigger.Toggle); // Child1 -> Child2 (OnExit async)
            acc ^= (int)_fastAsync.CurrentState;
        }
        BH(acc);
    }

    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("HSM-Async-Yield")]
    public async Task Stateless_Hsm_AsyncYield()
    {
        for (int i = 0; i < Ops; i++)
            await _slAsync.SM.FireAsync(HsmTrigger.Toggle);
        BenchmarkDotNet.Engines.DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_slAsync.SM);
    }

    // ---------- FASTFSM-ONLY: Shallow History ----------
    // Sekwencja: Outside->Parent(auto Child1)->Toggle(Child2)->LeaveParent->EnterParent (restore Child2)
    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("HSM-History-Shallow", "FastFSM-only")]
    public void FastFSM_Hsm_History_Shallow()
    {
        int acc = 0;
        for (int i = 0; i < Ops; i++)
        {
            _fastHist.TryFire(HsmTrigger.EnterParent); // -> Child1
            _fastHist.TryFire(HsmTrigger.Toggle);      // Child1 -> Child2
            _fastHist.TryFire(HsmTrigger.LeaveParent); // zapisz shallow history = Child2
            _fastHist.TryFire(HsmTrigger.EnterParent); // restore Child2
            acc ^= (int)_fastHist.CurrentState;
        }
        BH(acc);
    }
}