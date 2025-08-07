//using BenchmarkDotNet.Attributes;
//using BenchmarkDotNet.Jobs;
//using FastFsm.Abstractions;
//using System.Collections.Generic;
//using Abstractions.Attributes;

//namespace Benchmark;

//[SimpleJob(RuntimeMoniker.Net90)]
//[MemoryDiagnoser]
//public class HierarchicalStateMachineBenchmarks
//{
//    private FlatMachine _flatMachine = null!;
//    private HsmNoHistoryMachine _hsmNoHistory = null!;
//    private HsmShallowHistoryMachine _hsmShallowHistory = null!;
//    private HsmDeepHistoryMachine _hsmDeepHistory = null!;

//    [GlobalSetup]
//    public void Setup()
//    {
//        _flatMachine = new FlatMachine(BenchState.S1);
//        _hsmNoHistory = new HsmNoHistoryMachine(BenchState.S1);
//        _hsmShallowHistory = new HsmShallowHistoryMachine(BenchState.S1);
//        _hsmDeepHistory = new HsmDeepHistoryMachine(BenchState.S1);
//    }

//    [Benchmark(Baseline = true)]
//    public void FlatMachine_Transitions()
//    {
//        _flatMachine.Fire(BenchTrigger.Next);
//        _flatMachine.Fire(BenchTrigger.Next);
//        _flatMachine.Fire(BenchTrigger.Previous);
//        _flatMachine.Fire(BenchTrigger.Previous);
//    }

//    [Benchmark]
//    public void HsmNoHistory_Transitions()
//    {
//        _hsmNoHistory.Fire(BenchTrigger.Next);
//        _hsmNoHistory.Fire(BenchTrigger.Next);
//        _hsmNoHistory.Fire(BenchTrigger.Previous);
//        _hsmNoHistory.Fire(BenchTrigger.Previous);
//    }

//    [Benchmark]
//    public void HsmShallowHistory_Transitions()
//    {
//        _hsmShallowHistory.Fire(BenchTrigger.Next);
//        _hsmShallowHistory.Fire(BenchTrigger.ToOther);
//        _hsmShallowHistory.Fire(BenchTrigger.Back);
//        _hsmShallowHistory.Fire(BenchTrigger.Previous);
//    }

//    [Benchmark]
//    public void HsmDeepHistory_Transitions()
//    {
//        _hsmDeepHistory.Fire(BenchTrigger.Next);
//        _hsmDeepHistory.Fire(BenchTrigger.Deeper);
//        _hsmDeepHistory.Fire(BenchTrigger.ToOther);
//        _hsmDeepHistory.Fire(BenchTrigger.Back);
//    }

//    [Benchmark]
//    public bool FlatMachine_IsIn()
//    {
//        return _flatMachine.CurrentState == BenchState.S1;
//    }

//    [Benchmark]
//    public bool HsmNoHistory_IsIn()
//    {
//        return _hsmNoHistory.IsIn(BenchState.Group1);
//    }

//    [Benchmark]
//    public IReadOnlyList<BenchState> HsmNoHistory_GetActivePath()
//    {
//        return _hsmNoHistory.GetActivePath();
//    }

//    [Benchmark]
//    public bool FlatMachine_HasTransition()
//    {
//        return _flatMachine.HasTransition(BenchTrigger.Next);
//    }

//    [Benchmark]
//    public bool HsmNoHistory_HasTransition()
//    {
//        return _hsmNoHistory.HasTransition(BenchTrigger.Next);
//    }

//    [Benchmark]
//    public IEnumerable<BenchTrigger> FlatMachine_GetDefinedTriggers()
//    {
//        return _flatMachine.GetDefinedTriggers();
//    }

//    [Benchmark]
//    public IEnumerable<BenchTrigger> HsmNoHistory_GetDefinedTriggers()
//    {
//        return _hsmNoHistory.GetDefinedTriggers();
//    }
//}

//public enum BenchState
//{
//    Root,
//    Group1, S1, S2, S3,
//    Group2, S4, S5, S6,
//    Group3, S7, S8, S9,
//    Other,
//    Nested1, Nested2, Nested3
//}

//public enum BenchTrigger
//{
//    Next, Previous, ToOther, Back, Deeper
//}

//[StateMachine(typeof(BenchState), typeof(BenchTrigger), EnableHierarchy = false)]
//public partial class FlatMachine
//{
//    [State(BenchState.S1)]
//    void S1() { }

//    [State(BenchState.S2)]
//    void S2() { }

//    [State(BenchState.S3)]
//    void S3() { }

//    [State(BenchState.S4)]
//    void S4() { }

//    [Transition(BenchState.S1, BenchTrigger.Next, BenchState.S2)]
//    void S1ToS2() { }

//    [Transition(BenchState.S2, BenchTrigger.Next, BenchState.S3)]
//    void S2ToS3() { }

//    [Transition(BenchState.S3, BenchTrigger.Previous, BenchState.S2)]
//    void S3ToS2() { }

//    [Transition(BenchState.S2, BenchTrigger.Previous, BenchState.S1)]
//    void S2ToS1() { }
//}

//[StateMachine(typeof(BenchState), typeof(BenchTrigger), EnableHierarchy = true)]
//public partial class HsmNoHistoryMachine
//{
//    [State(BenchState.Root)]
//    void Root() { }

//    [State(BenchState.Group1, Parent = BenchState.Root)]
//    void Group1() { }

//    [State(BenchState.S1, Parent = BenchState.Group1)]
//    [InitialSubstate(BenchState.Group1, BenchState.S1)]
//    void S1() { }

//    [State(BenchState.S2, Parent = BenchState.Group1)]
//    void S2() { }

//    [State(BenchState.S3, Parent = BenchState.Group1)]
//    void S3() { }

//    [State(BenchState.Other, Parent = BenchState.Root)]
//    void Other() { }

//    [Transition(BenchState.S1, BenchTrigger.Next, BenchState.S2)]
//    void S1ToS2() { }

//    [Transition(BenchState.S2, BenchTrigger.Next, BenchState.S3)]
//    void S2ToS3() { }

//    [Transition(BenchState.S3, BenchTrigger.Previous, BenchState.S2)]
//    void S3ToS2() { }

//    [Transition(BenchState.S2, BenchTrigger.Previous, BenchState.S1)]
//    void S2ToS1() { }

//    [Transition(BenchState.Group1, BenchTrigger.ToOther, BenchState.Other)]
//    void GroupToOther() { }

//    [Transition(BenchState.Other, BenchTrigger.Back, BenchState.Group1)]
//    void OtherToGroup() { }
//}

//[StateMachine(typeof(BenchState), typeof(BenchTrigger), EnableHierarchy = true)]
//public partial class HsmShallowHistoryMachine
//{
//    [State(BenchState.Root)]
//    void Root() { }

//    [State(BenchState.Group1, Parent = BenchState.Root, History = HistoryMode.Shallow)]
//    void Group1() { }

//    [State(BenchState.S1, Parent = BenchState.Group1)]
//    [InitialSubstate(BenchState.Group1, BenchState.S1)]
//    void S1() { }

//    [State(BenchState.S2, Parent = BenchState.Group1)]
//    void S2() { }

//    [State(BenchState.S3, Parent = BenchState.Group1)]
//    void S3() { }

//    [State(BenchState.Other, Parent = BenchState.Root)]
//    void Other() { }

//    [Transition(BenchState.S1, BenchTrigger.Next, BenchState.S2)]
//    void S1ToS2() { }

//    [Transition(BenchState.S2, BenchTrigger.Next, BenchState.S3)]
//    void S2ToS3() { }

//    [Transition(BenchState.S3, BenchTrigger.Previous, BenchState.S2)]
//    void S3ToS2() { }

//    [Transition(BenchState.S2, BenchTrigger.Previous, BenchState.S1)]
//    void S2ToS1() { }

//    [Transition(BenchState.Group1, BenchTrigger.ToOther, BenchState.Other)]
//    void GroupToOther() { }

//    [Transition(BenchState.Other, BenchTrigger.Back, BenchState.Group1)]
//    void OtherToGroup() { }
//}

//[StateMachine(typeof(BenchState), typeof(BenchTrigger), EnableHierarchy = true)]
//public partial class HsmDeepHistoryMachine
//{
//    [State(BenchState.Root)]
//    void Root() { }

//    [State(BenchState.Group1, Parent = BenchState.Root, History = HistoryMode.Deep)]
//    void Group1() { }

//    [State(BenchState.S1, Parent = BenchState.Group1)]
//    [InitialSubstate(BenchState.Group1, BenchState.S1)]
//    void S1() { }

//    [State(BenchState.S2, Parent = BenchState.Group1, History = HistoryMode.Deep)]
//    void S2() { }

//    [State(BenchState.Nested1, Parent = BenchState.S2)]
//    [InitialSubstate(BenchState.S2, BenchState.Nested1)]
//    void Nested1() { }

//    [State(BenchState.Nested2, Parent = BenchState.S2)]
//    void Nested2() { }

//    [State(BenchState.Nested3, Parent = BenchState.S2)]
//    void Nested3() { }

//    [State(BenchState.Other, Parent = BenchState.Root)]
//    void Other() { }

//    [Transition(BenchState.S1, BenchTrigger.Next, BenchState.S2)]
//    void S1ToS2() { }

//    [Transition(BenchState.Nested1, BenchTrigger.Deeper, BenchState.Nested2)]
//    void N1ToN2() { }

//    [Transition(BenchState.Nested2, BenchTrigger.Deeper, BenchState.Nested3)]
//    void N2ToN3() { }

//    [Transition(BenchState.Group1, BenchTrigger.ToOther, BenchState.Other)]
//    void GroupToOther() { }

//    [Transition(BenchState.Other, BenchTrigger.Back, BenchState.Group1)]
//    void OtherToGroup() { }
//}