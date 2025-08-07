using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Columns;

// FastFSM
using Abstractions.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using LiquidState;

// Stateless
using Stateless;

// LiquidState aliases
using LS = LiquidState.Synchronous.Core;
using LSA = LiquidState.Awaitable.Core;
using LSC = LiquidState.Core;

// Appccelerate aliases
using AC = Appccelerate.StateMachine;                 // root (PassiveStateMachine, AsyncPassiveStateMachine, etc.)
using ACCM = Appccelerate.StateMachine.Machine;       // sync builder
using ACCA = Appccelerate.StateMachine.AsyncMachine;  // async builder

namespace Benchmark
{
    // ===== Shared enums & payload =====
    public enum State { A, B, C }
    public enum Trigger { Next }

    public class PayloadData
    {
        public int Value { get; set; }
        public string Message { get; set; } = "";
    }

    // ===== FastFSM implementations =====
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class FastFsmBasic
    {
        [Transition(State.A, Trigger.Next, State.B)]
        [Transition(State.B, Trigger.Next, State.C)]
        [Transition(State.C, Trigger.Next, State.A)]
        private void Configure() { }
    }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class FastFsmWithGuardsActions
    {
        private int _counter;
        private const int GuardLimit = int.MaxValue; // ujednolicone z innymi implementacjami

        [Transition(State.A, Trigger.Next, State.B, Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
        [Transition(State.B, Trigger.Next, State.C, Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
        [Transition(State.C, Trigger.Next, State.A, Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
        private void Configure() { }

        private bool CanTransition() => _counter < GuardLimit;
        private void IncrementCounter() => _counter++;
    }

    [StateMachine(typeof(State), typeof(Trigger))]
    [PayloadType(typeof(PayloadData))]
    public partial class FastFsmWithPayload
    {
        private int _sum;

        [Transition(State.A, Trigger.Next, State.B, Action = nameof(ProcessPayload))]
        [Transition(State.B, Trigger.Next, State.C, Action = nameof(ProcessPayload))]
        [Transition(State.C, Trigger.Next, State.A, Action = nameof(ProcessPayload))]
        private void Configure() { }

        private void ProcessPayload(PayloadData data) => _sum += data.Value;
    }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class FastFsmAsyncActions
    {
        private int _asyncCounter;

        // Wariant "real async" (scheduler hop). Do hot-path patrz niżej w benchmarkach (metoda bez yield).
        [Transition(State.A, Trigger.Next, State.B, Action = nameof(ProcessAsyncYield))]
        [Transition(State.B, Trigger.Next, State.C, Action = nameof(ProcessAsyncYield))]
        [Transition(State.C, Trigger.Next, State.A, Action = nameof(ProcessAsyncYield))]
        private void Configure() { }

        private async ValueTask ProcessAsyncYield()
        {
            await Task.Yield();
            _asyncCounter++;
        }

        // Dodatkowy callback do hot-path (bez przełączania kontekstu)
        private ValueTask ProcessAsyncCompleted()
        {
            _asyncCounter++;
            return ValueTask.CompletedTask;
        }
    }

    // ===== Benchmarks =====
    // Uwaga: HardwareCounters wymagają Windows, uprawnień Administratora i braku Hyper-V.
    // W przeciwnym razie kolumny CPU nie pojawią się w raporcie.
    [SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 15)]
    [MemoryDiagnoser]
    [DisassemblyDiagnoser(maxDepth: 3)]
  
    public class StateMachineBenchmarks
    {
        private const int Ops = 1024; // liczba operacji na jedno wywołanie benchmarku

        // ---------- Stateless ----------
        private Stateless.StateMachine<State, Trigger> _statelessBasic = null!;
        private Stateless.StateMachine<State, Trigger> _statelessGuardsActions = null!;
        private Stateless.StateMachine<State, Trigger> _statelessPayload = null!;
        private Stateless.StateMachine<State, Trigger>.TriggerWithParameters<PayloadData> _statelessPayloadTrigger = null!;
        private Stateless.StateMachine<State, Trigger> _statelessAsyncActions = null!;
        private int _statelessCounter;
        private int _statelessPayloadSum;
        private int _statelessAsyncCounter;

        // ---------- FastFSM ----------
        private FastFsmBasic _fastFsmBasic = null!;
        private FastFsmWithGuardsActions _fastFsmGuardsActions = null!;
        private FastFsmWithPayload _fastFsmPayload = null!;
        private FastFsmAsyncActions _fastFsmAsyncActions = null!;

        // ---------- LiquidState ----------
        private LS.IStateMachine<State, Trigger> _liquidStateBasic = null!;
        private LS.IStateMachine<State, Trigger> _liquidStatePayload = null!;
        private LSC.ParameterizedTrigger<Trigger, PayloadData> _liquidStatePayloadTrigger = null!;
        private LSA.IAwaitableStateMachine<State, Trigger> _liquidStateAsyncActions = null!;
        private int _liquidStatePayloadSum;
        private int _liquidStateAsyncCounter;

        // ---------- Appccelerate ----------
        private AC.PassiveStateMachine<State, Trigger> _appccBasic = null!;
        private AC.PassiveStateMachine<State, Trigger> _appccGuards = null!;
        private AC.PassiveStateMachine<State, Trigger> _appccPayload = null!;
        private AC.AsyncPassiveStateMachine<State, Trigger> _appccAsync = null!;
        private int _appccCounter;
        private int _appccPayloadSum;
        private int _appccAsyncCounter;

        // ---------- Shared ----------
        private PayloadData _payloadData = null!;
        private const int GuardLimit = int.MaxValue;

        // Helpers for Stateless guards/actions
        private bool CanTransition() => _statelessCounter < GuardLimit;
        private void IncrementCounter() => _statelessCounter++;

        [GlobalSetup]
        public async Task Setup()
        {
            // ===== FastFSM =====
            _fastFsmBasic = new FastFsmBasic(State.A);
            _fastFsmBasic.Start();
            _fastFsmGuardsActions = new FastFsmWithGuardsActions(State.A);
            _fastFsmGuardsActions.Start();
            _fastFsmPayload = new FastFsmWithPayload(State.A);
            _fastFsmPayload.Start();
            _fastFsmAsyncActions = new FastFsmAsyncActions(State.A);
            await _fastFsmAsyncActions.StartAsync();

            // ===== Stateless =====
            _statelessBasic = new Stateless.StateMachine<State, Trigger>(State.A);
            _statelessBasic.Configure(State.A).Permit(Trigger.Next, State.B);
            _statelessBasic.Configure(State.B).Permit(Trigger.Next, State.C);
            _statelessBasic.Configure(State.C).Permit(Trigger.Next, State.A);

            _statelessGuardsActions = new Stateless.StateMachine<State, Trigger>(State.A);
            _statelessGuardsActions.Configure(State.A)
                .PermitIf(Trigger.Next, State.B, CanTransition)
                .OnExit(IncrementCounter);
            _statelessGuardsActions.Configure(State.B)
                .PermitIf(Trigger.Next, State.C, CanTransition)
                .OnExit(IncrementCounter);
            _statelessGuardsActions.Configure(State.C)
                .PermitIf(Trigger.Next, State.A, CanTransition)
                .OnExit(IncrementCounter);

            _statelessPayload = new Stateless.StateMachine<State, Trigger>(State.A);
            _statelessPayloadTrigger = _statelessPayload.SetTriggerParameters<PayloadData>(Trigger.Next);
            _statelessPayload.Configure(State.A)
                .Permit(Trigger.Next, State.B)
                .OnEntryFrom(_statelessPayloadTrigger, d => _statelessPayloadSum += d.Value);
            _statelessPayload.Configure(State.B)
                .Permit(Trigger.Next, State.C)
                .OnEntryFrom(_statelessPayloadTrigger, d => _statelessPayloadSum += d.Value);
            _statelessPayload.Configure(State.C)
                .Permit(Trigger.Next, State.A)
                .OnEntryFrom(_statelessPayloadTrigger, d => _statelessPayloadSum += d.Value);

            _statelessAsyncActions = new Stateless.StateMachine<State, Trigger>(State.A);
            async Task DoAsync()
            {
                await Task.Yield();
                _statelessAsyncCounter++;
            }
            _statelessAsyncActions.Configure(State.A)
                .Permit(Trigger.Next, State.B)
                .OnExitAsync(DoAsync);
            _statelessAsyncActions.Configure(State.B)
                .Permit(Trigger.Next, State.C)
                .OnExitAsync(DoAsync);
            _statelessAsyncActions.Configure(State.C)
                .Permit(Trigger.Next, State.A)
                .OnExitAsync(DoAsync);

            // ===== LiquidState =====
            var liquidStateBasicConfig = StateMachineFactory.CreateConfiguration<State, Trigger>();
            liquidStateBasicConfig.ForState(State.A).Permit(Trigger.Next, State.B);
            liquidStateBasicConfig.ForState(State.B).Permit(Trigger.Next, State.C);
            liquidStateBasicConfig.ForState(State.C).Permit(Trigger.Next, State.A);
            _liquidStateBasic = StateMachineFactory.Create(State.A, liquidStateBasicConfig);

            var liquidStatePayloadConfig = StateMachineFactory.CreateConfiguration<State, Trigger>();
            _liquidStatePayloadTrigger = liquidStatePayloadConfig.SetTriggerParameter<PayloadData>(Trigger.Next);
            liquidStatePayloadConfig.ForState(State.A)
                .Permit(_liquidStatePayloadTrigger, State.B, d => _liquidStatePayloadSum += d.Value);
            liquidStatePayloadConfig.ForState(State.B)
                .Permit(_liquidStatePayloadTrigger, State.C, d => _liquidStatePayloadSum += d.Value);
            liquidStatePayloadConfig.ForState(State.C)
                .Permit(_liquidStatePayloadTrigger, State.A, d => _liquidStatePayloadSum += d.Value);
            _liquidStatePayload = StateMachineFactory.Create(State.A, liquidStatePayloadConfig);

            var liquidStateAsyncConfig = StateMachineFactory.CreateAwaitableConfiguration<State, Trigger>();
            async Task DoLiquidAsync()
            {
                await Task.Yield();
                _liquidStateAsyncCounter++;
            }
            liquidStateAsyncConfig.ForState(State.A)
                .Permit(Trigger.Next, State.B)
                .OnExit(DoLiquidAsync);
            liquidStateAsyncConfig.ForState(State.B)
                .Permit(Trigger.Next, State.C)
                .OnExit(DoLiquidAsync);
            liquidStateAsyncConfig.ForState(State.C)
                .Permit(Trigger.Next, State.A)
                .OnExit(DoLiquidAsync);
            _liquidStateAsyncActions = StateMachineFactory.Create(State.A, liquidStateAsyncConfig);

            // ===== Appccelerate =====
            // Basic
            var b = new ACCM.StateMachineDefinitionBuilder<State, Trigger>();
            b.WithInitialState(State.A);
            b.In(State.A).On(Trigger.Next).Goto(State.B);
            b.In(State.B).On(Trigger.Next).Goto(State.C);
            b.In(State.C).On(Trigger.Next).Goto(State.A);
            _appccBasic = b.Build().CreatePassiveStateMachine();
            _appccBasic.Start();

            // Guards + Actions
            var g = new ACCM.StateMachineDefinitionBuilder<State, Trigger>();
            g.WithInitialState(State.A);
            g.In(State.A).On(Trigger.Next)
                .If(() => _appccCounter < GuardLimit).Goto(State.B)
                .Execute(() => _appccCounter++);
            g.In(State.B).On(Trigger.Next)
                .If(() => _appccCounter < GuardLimit).Goto(State.C)
                .Execute(() => _appccCounter++);
            g.In(State.C).On(Trigger.Next)
                .If(() => _appccCounter < GuardLimit).Goto(State.A)
                .Execute(() => _appccCounter++);
            _appccGuards = g.Build().CreatePassiveStateMachine();
            _appccGuards.Start();

            // Payload
            _payloadData = new PayloadData { Value = 42, Message = "Test" };
            var p = new ACCM.StateMachineDefinitionBuilder<State, Trigger>();
            p.WithInitialState(State.A);
            p.In(State.A).On(Trigger.Next).Goto(State.B).Execute<PayloadData>(d => _appccPayloadSum += d.Value);
            p.In(State.B).On(Trigger.Next).Goto(State.C).Execute<PayloadData>(d => _appccPayloadSum += d.Value);
            p.In(State.C).On(Trigger.Next).Goto(State.A).Execute<PayloadData>(d => _appccPayloadSum += d.Value);
            _appccPayload = p.Build().CreatePassiveStateMachine();
            _appccPayload.Start();

            // Async
            var a = new ACCA.StateMachineDefinitionBuilder<State, Trigger>();
            a.WithInitialState(State.A);
            a.In(State.A).On(Trigger.Next).Goto(State.B).Execute(async () =>
            {
                await Task.Yield();
                _appccAsyncCounter++;
            });
            a.In(State.B).On(Trigger.Next).Goto(State.C).Execute(async () =>
            {
                await Task.Yield();
                _appccAsyncCounter++;
            });
            a.In(State.C).On(Trigger.Next).Goto(State.A).Execute(async () =>
            {
                await Task.Yield();
                _appccAsyncCounter++;
            });

            var asyncDef = a.Build();
            _appccAsync = asyncDef.CreatePassiveStateMachine();
            _appccAsync.Start().GetAwaiter().GetResult();
        }

        // ===== Basic =====
        [Benchmark(Baseline = true, OperationsPerInvoke = Ops), BenchmarkCategory("Basic")]
        public void Stateless_Basic()
        {
            for (int i = 0; i < Ops; i++) _statelessBasic.Fire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_statelessBasic);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Basic")]
        public void FastFsm_Basic()
        {
            for (int i = 0; i < Ops; i++) _fastFsmBasic.TryFire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_fastFsmBasic.CurrentState);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Basic")]
        public void LiquidState_Basic()
        {
            for (int i = 0; i < Ops; i++) _liquidStateBasic.Fire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_liquidStateBasic);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Basic")]
        public void Appccelerate_Basic()
        {
            for (int i = 0; i < Ops; i++) _appccBasic.Fire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_appccBasic);
        }

        // ===== Guards & Actions =====
        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("GuardsActions")]
        public void Stateless_GuardsActions()
        {
            for (int i = 0; i < Ops; i++) _statelessGuardsActions.Fire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_statelessCounter);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("GuardsActions")]
        public void FastFsm_GuardsActions()
        {
            for (int i = 0; i < Ops; i++) _fastFsmGuardsActions.TryFire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_fastFsmGuardsActions.CurrentState);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("GuardsActions")]
        public void Appccelerate_GuardsActions()
        {
            for (int i = 0; i < Ops; i++) _appccGuards.Fire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_appccCounter);
        }

        // ===== Payload =====
        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Payload")]
        public void Stateless_Payload()
        {
            for (int i = 0; i < Ops; i++) _statelessPayload.Fire(_statelessPayloadTrigger, _payloadData);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_statelessPayloadSum);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Payload")]
        public void FastFsm_Payload()
        {
            for (int i = 0; i < Ops; i++) _fastFsmPayload.TryFire(Trigger.Next, _payloadData);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_fastFsmPayload.CurrentState);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Payload")]
        public void LiquidState_Payload()
        {
            for (int i = 0; i < Ops; i++) _liquidStatePayload.Fire(_liquidStatePayloadTrigger, _payloadData);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_liquidStatePayloadSum);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Payload")]
        public void Appccelerate_Payload()
        {
            for (int i = 0; i < Ops; i++) _appccPayload.Fire(Trigger.Next, _payloadData);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_appccPayloadSum);
        }

        // ===== Async (real async: Task.Yield) =====
        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Async-Yield")]
        public async Task Stateless_AsyncActions()
        {
            for (int i = 0; i < Ops; i++) await _statelessAsyncActions.FireAsync(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_statelessAsyncCounter);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Async-Yield")]
        public async ValueTask FastFsm_AsyncActions()
        {
            for (int i = 0; i < Ops; i++) await _fastFsmAsyncActions.TryFireAsync(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_fastFsmAsyncActions.CurrentState);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Async-Yield")]
        public async Task LiquidState_AsyncActions()
        {
            for (int i = 0; i < Ops; i++) await _liquidStateAsyncActions.FireAsync(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_liquidStateAsyncCounter);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Async-Yield")]
        public async Task Appccelerate_AsyncActions()
        {
            for (int i = 0; i < Ops; i++) await _appccAsync.Fire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_appccAsyncCounter);
        }

        // ===== Async (hot path: CompletedTask / brak yield) — opcjonalnie do porównania narzutu frameworku
        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Async-HotPath")]
        public async ValueTask FastFsm_AsyncActions_HotPath()
        {
            // Re-mapuj w generatorze akcję do FastFsmAsyncActions.ProcessAsyncCompleted, aby użyć CompletedTask
            for (int i = 0; i < Ops; i++) await _fastFsmAsyncActions.TryFireAsync(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_fastFsmAsyncActions.CurrentState);
        }

        // ===== Helper-ish (API only where available) =====
        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Helper")]
        public void Stateless_CanFire()
        {
            for (int i = 0; i < Ops; i++) _ = _statelessBasic.CanFire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_statelessBasic);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Helper")]
        public void FastFsm_CanFire()
        {
            for (int i = 0; i < Ops; i++) _ = _fastFsmBasic.CanFire(Trigger.Next);
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_fastFsmBasic.CurrentState);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Helper")]
        public void Stateless_GetPermittedTriggers()
        {
            for (int i = 0; i < Ops; i++) _ = _statelessBasic.PermittedTriggers;
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_statelessBasic);
        }

        [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory("Helper")]
        public void FastFsm_GetPermittedTriggers()
        {
            for (int i = 0; i < Ops; i++) _ = _fastFsmBasic.GetPermittedTriggers();
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(_fastFsmBasic);
        }
    }
}
