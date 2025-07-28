//// MemoryBenchmarks.cs - Osobny benchmark dla alokacji

//using Abstractions.Attributes;
//using BenchmarkDotNet.Attributes;
//using Stateless;
//using static StateMachineBenchmarks;

//[MemoryDiagnoser]
//[SimpleJob(warmupCount: 3, iterationCount: 5)]
//public class MemoryBenchmarks
//{
//    // ===== Basic State Machine =====

//    // FastFSM
//    [StateMachine(typeof(State), typeof(Trigger))]
//    public partial class FastFsmBasic
//    {
//        [Transition(State.A, Trigger.Next, State.B)]
//        [Transition(State.B, Trigger.Next, State.C)]
//        [Transition(State.C, Trigger.Next, State.A)]
//        private void Configure() { }
//    }

//    // Stateless
//    private StateMachine<State, Trigger> _statelessBasic = null!;
//    private FastFsmBasic _fastFsmBasic = null!;

//    // ===== With Guards and Actions =====

//    [StateMachine(typeof(State), typeof(Trigger))]
//    public partial class FastFsmWithGuardsActions
//    {
//        private int _counter;

//        [Transition(State.A, Trigger.Next, State.B,
//            Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
//        [Transition(State.B, Trigger.Next, State.C,
//            Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
//        [Transition(State.C, Trigger.Next, State.A,
//            Guard = nameof(CanTransition), Action = nameof(IncrementCounter))]
//        private void Configure() { }

//        private bool CanTransition() => _counter < 1000000;
//        private void IncrementCounter() => _counter++;
//    }

//    private StateMachine<State, Trigger> _statelessGuardsActions = null!;
//    private FastFsmWithGuardsActions _fastFsmGuardsActions = null!;
//    private int _statelessCounter;

//    // ===== With Payload =====

//    public class PayloadData
//    {
//        public int Value { get; set; }
//        public string Message { get; set; } = "";
//    }

//    [StateMachine(typeof(State), typeof(Trigger))]
//    [PayloadType(typeof(PayloadData))]
//    public partial class FastFsmWithPayload
//    {
//        private int _sum;

//        [Transition(State.A, Trigger.Next, State.B, Action = nameof(ProcessPayload))]
//        [Transition(State.B, Trigger.Next, State.C, Action = nameof(ProcessPayload))]
//        [Transition(State.C, Trigger.Next, State.A, Action = nameof(ProcessPayload))]
//        private void Configure() { }

//        private void ProcessPayload(PayloadData data) => _sum += data.Value;
//    }

//    private StateMachine<State, Trigger>.TriggerWithParameters<PayloadData> _statelessPayloadTrigger = null!;
//    private StateMachine<State, Trigger> _statelessPayload = null!;
//    private FastFsmWithPayload _fastFsmPayload = null!;
//    private PayloadData _payloadData = null!;
//    private int _statelessPayloadSum;

//    // ===== Async =====

//    [StateMachine(typeof(State), typeof(Trigger))]
//    public partial class FastFsmAsync
//    {
//        private int _asyncCounter;

//        [Transition(State.A, Trigger.Next, State.B,
//            Guard = nameof(CanTransitionAsync), Action = nameof(ProcessAsync))]
//        [Transition(State.B, Trigger.Next, State.C,
//            Guard = nameof(CanTransitionAsync), Action = nameof(ProcessAsync))]
//        [Transition(State.C, Trigger.Next, State.A,
//            Guard = nameof(CanTransitionAsync), Action = nameof(ProcessAsync))]
//        private void Configure() { }

//        private async ValueTask<bool> CanTransitionAsync()
//        {
//            await Task.Yield();
//            return _asyncCounter < 1000000;
//        }

//        private async Task ProcessAsync()
//        {
//            await Task.Yield();
//            _asyncCounter++;
//        }
//    }

//    private StateMachine<State, Trigger> _statelessAsync = null!;
//    private FastFsmAsync _fastFsmAsync = null!;
//    private int _statelessAsyncCounter;

//    public enum State { A, B, C }
//    public enum Trigger { Next }

//    [Params(100, 1000, 10000)]
//    public int Iterations { get; set; }

//    private FastFsmBasic _fastFsm = null!;
//    private StateMachine<State, Trigger> _stateless = null!;

//    [GlobalSetup]
//    public void Setup()
//    {
//        // Basic setup
//        _fastFsmBasic = new FastFsmBasic(State.A);
//        _statelessBasic = new StateMachine<State, Trigger>(State.A);
//        _statelessBasic.Configure(State.A).Permit(Trigger.Next, State.B);
//        _statelessBasic.Configure(State.B).Permit(Trigger.Next, State.C);
//        _statelessBasic.Configure(State.C).Permit(Trigger.Next, State.A);

//        // Guards & Actions setup
//        _fastFsmGuardsActions = new FastFsmWithGuardsActions(State.A);
//        _statelessGuardsActions = new StateMachine<State, Trigger>(State.A);
//        _statelessGuardsActions.Configure(State.A)
//            .PermitIf(Trigger.Next, State.B, () => _statelessCounter < 1000000)
//            .OnExit(() => _statelessCounter++);
//        _statelessGuardsActions.Configure(State.B)
//            .PermitIf(Trigger.Next, State.C, () => _statelessCounter < 1000000)
//            .OnExit(() => _statelessCounter++);
//        _statelessGuardsActions.Configure(State.C)
//            .PermitIf(Trigger.Next, State.A, () => _statelessCounter < 1000000)
//            .OnExit(() => _statelessCounter++);

//        // Payload setup
//        _fastFsmPayload = new FastFsmWithPayload(State.A);
//        _statelessPayload = new StateMachine<State, Trigger>(State.A);
//        _statelessPayloadTrigger = _statelessPayload.SetTriggerParameters<PayloadData>(Trigger.Next);

//        _statelessPayload.Configure(State.A)
//            .Permit(Trigger.Next, State.B)
//            .OnEntryFrom(_statelessPayloadTrigger, data => _statelessPayloadSum += data.Value);
//        _statelessPayload.Configure(State.B)
//            .Permit(Trigger.Next, State.C)
//            .OnEntryFrom(_statelessPayloadTrigger, data => _statelessPayloadSum += data.Value);
//        _statelessPayload.Configure(State.C)
//            .Permit(Trigger.Next, State.A)
//            .OnEntryFrom(_statelessPayloadTrigger, data => _statelessPayloadSum += data.Value);

//        _payloadData = new PayloadData { Value = 42, Message = "Test" };

//        // Async setup
//        _fastFsmAsync = new FastFsmAsync(State.A);
//        _statelessAsync = new StateMachine<State, Trigger>(State.A);
//        _statelessAsync.Configure(State.A)
//            .PermitIf(Trigger.Next, State.B, async () =>
//            {
//                await Task.Yield();
//                return _statelessAsyncCounter < 1000000;
//            })
//            .OnExitAsync(async () =>
//            {
//                await Task.Yield();
//                _statelessAsyncCounter++;
//            });
//        _statelessAsync.Configure(State.B)
//            .PermitIf(Trigger.Next, State.C, async () =>
//            {
//                await Task.Yield();
//                return _statelessAsyncCounter < 1000000;
//            })
//            .OnExitAsync(async () =>
//            {
//                await Task.Yield();
//                _statelessAsyncCounter++;
//            });
//        _statelessAsync.Configure(State.C)
//            .PermitIf(Trigger.Next, State.A, async () =>
//            {
//                await Task.Yield();
//                return _statelessAsyncCounter < 1000000;
//            })
//            .OnExitAsync(async () =>
//            {
//                await Task.Yield();
//                _statelessAsyncCounter++;
//            });
//    }

//    [Benchmark]
//    public void Stateless_MultipleTransitions()
//    {
//        for (int i = 0; i < Iterations; i++)
//        {
//            _stateless.Fire(Trigger.Next);
//        }
//    }

//    [Benchmark]
//    public void FastFsm_MultipleTransitions()
//    {
//        for (int i = 0; i < Iterations; i++)
//        {
//            _fastFsm.TryFire(Trigger.Next);
//        }
//    }
//}