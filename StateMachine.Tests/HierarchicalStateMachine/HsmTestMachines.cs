//using System;
//using System.Collections.Generic;
//using Abstractions.Attributes;
//using StateMachine.Exceptions;

//namespace StateMachine.Tests.HierarchicalStateMachine;

//public enum HsmState
//{
//    Root,
//    A, A1, A2, A3,
//    B, B1, B2, B3,
//    C, C1, C2,
//    D
//}

//public enum HsmTrigger
//{
//    Next,
//    Previous,
//    ToA,
//    ToB,
//    ToC,
//    ToD,
//    ToA1,
//    ToA2,
//    ToB1,
//    ToB2,
//    ToC1,
//    ToC2,
//    Internal,
//    External
//}

//[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
//public partial class BasicHierarchicalMachine
//{
//    public List<string> ExecutionLog { get; } = new();

//    [State(HsmState.Root)]
//    void Root() { }

//    [State(HsmState.A, Parent = HsmState.Root, OnEntry = nameof(OnEntryA), OnExit = nameof(OnExitA))]
//    void A() { }

//    [State(HsmState.A1, Parent = HsmState.A, OnEntry = nameof(OnEntryA1), OnExit = nameof(OnExitA1), IsInitial = true)]
//    // [InitialSubstate(HsmState.A, HsmState.A1)] - deprecated, use IsInitial in State attribute
//    void A1() { }

//    [State(HsmState.A2, Parent = HsmState.A, OnEntry = nameof(OnEntryA2), OnExit = nameof(OnExitA2))]
//    void A2() { }

//    [State(HsmState.A3, Parent = HsmState.A, OnEntry = nameof(OnEntryA3), OnExit = nameof(OnExitA3))]
//    void A3() { }

//    [State(HsmState.B, Parent = HsmState.Root, OnEntry = nameof(OnEntryB), OnExit = nameof(OnExitB))]
//    void B() { }

//    [State(HsmState.B1, Parent = HsmState.B, OnEntry = nameof(OnEntryB1), OnExit = nameof(OnExitB1), IsInitial = true)]
//    // [InitialSubstate(HsmState.B, HsmState.B1)] - deprecated, use IsInitial in State attribute
//    void B1() { }

//    [State(HsmState.B2, Parent = HsmState.B, OnEntry = nameof(OnEntryB2), OnExit = nameof(OnExitB2))]
//    void B2() { }

//    [State(HsmState.B3, Parent = HsmState.B, OnEntry = nameof(OnEntryB3), OnExit = nameof(OnExitB3))]
//    void B3() { }

//    [State(HsmState.C, Parent = HsmState.Root, OnEntry = nameof(OnEntryC), OnExit = nameof(OnExitC))]
//    void C() { }

//    [State(HsmState.C1, Parent = HsmState.C, OnEntry = nameof(OnEntryC1), OnExit = nameof(OnExitC1), IsInitial = true)]
//    // [InitialSubstate(HsmState.C, HsmState.C1)] - deprecated, use IsInitial in State attribute
//    void C1() { }

//    [State(HsmState.C2, Parent = HsmState.C, OnEntry = nameof(OnEntryC2), OnExit = nameof(OnExitC2))]
//    void C2() { }

//    [State(HsmState.D, Parent = HsmState.Root, OnEntry = nameof(OnEntryD), OnExit = nameof(OnExitD))]
//    void D() { }

//    // Transitions defined at different levels
//    [Transition(HsmState.Root, HsmTrigger.ToD, HsmState.D)]
//    void RootToD() => ExecutionLog.Add("Action-Root-to-D");

//    [Transition(HsmState.A, HsmTrigger.ToB, HsmState.B)]
//    void AToB() => ExecutionLog.Add("Action-A-to-B");

//    [Transition(HsmState.A1, HsmTrigger.Next, HsmState.A2)]
//    void A1ToA2() => ExecutionLog.Add("Action-A1-to-A2");

//    [Transition(HsmState.A2, HsmTrigger.Next, HsmState.A3)]
//    void A2ToA3() => ExecutionLog.Add("Action-A2-to-A3");

//    [Transition(HsmState.B, HsmTrigger.ToA, HsmState.A)]
//    void BToA() => ExecutionLog.Add("Action-B-to-A");

//    [Transition(HsmState.B1, HsmTrigger.Next, HsmState.B2)]
//    void B1ToB2() => ExecutionLog.Add("Action-B1-to-B2");

//    [Transition(HsmState.B2, HsmTrigger.Next, HsmState.B3)]
//    void B2ToB3() => ExecutionLog.Add("Action-B2-to-B3");

//    [Transition(HsmState.C, HsmTrigger.ToA, HsmState.A)]
//    void CToA() => ExecutionLog.Add("Action-C-to-A");

//    [Transition(HsmState.C1, HsmTrigger.Next, HsmState.C2)]
//    void C1ToC2() => ExecutionLog.Add("Action-C1-to-C2");

//    // Specific transitions to substates
//    [Transition(HsmState.D, HsmTrigger.ToA1, HsmState.A1)]
//    void DToA1() => ExecutionLog.Add("Action-D-to-A1");

//    [Transition(HsmState.D, HsmTrigger.ToB2, HsmState.B2)]
//    void DToB2() => ExecutionLog.Add("Action-D-to-B2");

//    // Callbacks
//    void OnEntryA() => ExecutionLog.Add("Entry-A");
//    void OnExitA() => ExecutionLog.Add("Exit-A");
//    void OnEntryA1() => ExecutionLog.Add("Entry-A1");
//    void OnExitA1() => ExecutionLog.Add("Exit-A1");
//    void OnEntryA2() => ExecutionLog.Add("Entry-A2");
//    void OnExitA2() => ExecutionLog.Add("Exit-A2");
//    void OnEntryA3() => ExecutionLog.Add("Entry-A3");
//    void OnExitA3() => ExecutionLog.Add("Exit-A3");

//    void OnEntryB() => ExecutionLog.Add("Entry-B");
//    void OnExitB() => ExecutionLog.Add("Exit-B");
//    void OnEntryB1() => ExecutionLog.Add("Entry-B1");
//    void OnExitB1() => ExecutionLog.Add("Exit-B1");
//    void OnEntryB2() => ExecutionLog.Add("Entry-B2");
//    void OnExitB2() => ExecutionLog.Add("Exit-B2");
//    void OnEntryB3() => ExecutionLog.Add("Entry-B3");
//    void OnExitB3() => ExecutionLog.Add("Exit-B3");

//    void OnEntryC() => ExecutionLog.Add("Entry-C");
//    void OnExitC() => ExecutionLog.Add("Exit-C");
//    void OnEntryC1() => ExecutionLog.Add("Entry-C1");
//    void OnExitC1() => ExecutionLog.Add("Exit-C1");
//    void OnEntryC2() => ExecutionLog.Add("Entry-C2");
//    void OnExitC2() => ExecutionLog.Add("Exit-C2");

//    void OnEntryD() => ExecutionLog.Add("Entry-D");
//    void OnExitD() => ExecutionLog.Add("Exit-D");
//}

//[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
//public partial class ShallowHistoryMachine
//{
//    public List<string> ExecutionLog { get; } = new();

//    [State(HsmState.Root)]
//    void Root() { }

//    [State(HsmState.A, Parent = HsmState.Root, History = HistoryMode.Shallow, OnEntry = nameof(OnEntryA), OnExit = nameof(OnExitA))]
//    void A() { }

//    [State(HsmState.A1, Parent = HsmState.A, OnEntry = nameof(OnEntryA1), OnExit = nameof(OnExitA1), IsInitial = true)]
//    // [InitialSubstate(HsmState.A, HsmState.A1)] - deprecated, use IsInitial in State attribute
//    void A1() { }

//    [State(HsmState.A2, Parent = HsmState.A, OnEntry = nameof(OnEntryA2), OnExit = nameof(OnExitA2))]
//    void A2() { }

//    [State(HsmState.B, Parent = HsmState.Root, OnEntry = nameof(OnEntryB), OnExit = nameof(OnExitB))]
//    void B() { }

//    [Transition(HsmState.A1, HsmTrigger.Next, HsmState.A2)]
//    void A1ToA2() => ExecutionLog.Add("Action-A1-to-A2");

//    [Transition(HsmState.A, HsmTrigger.ToB, HsmState.B)]
//    void AToB() => ExecutionLog.Add("Action-A-to-B");

//    [Transition(HsmState.B, HsmTrigger.ToA, HsmState.A)]
//    void BToA() => ExecutionLog.Add("Action-B-to-A");

//    void OnEntryA() => ExecutionLog.Add("Entry-A");
//    void OnExitA() => ExecutionLog.Add("Exit-A");
//    void OnEntryA1() => ExecutionLog.Add("Entry-A1");
//    void OnExitA1() => ExecutionLog.Add("Exit-A1");
//    void OnEntryA2() => ExecutionLog.Add("Entry-A2");
//    void OnExitA2() => ExecutionLog.Add("Exit-A2");
//    void OnEntryB() => ExecutionLog.Add("Entry-B");
//    void OnExitB() => ExecutionLog.Add("Exit-B");
//}

//[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
//public partial class DeepHistoryMachine
//{
//    public List<string> ExecutionLog { get; } = new();

//    [State(HsmState.Root)]
//    void Root() { }

//    [State(HsmState.A, Parent = HsmState.Root, History = HistoryMode.Deep, OnEntry = nameof(OnEntryA), OnExit = nameof(OnExitA))]
//    void A() { }

//    [State(HsmState.A1, Parent = HsmState.A, OnEntry = nameof(OnEntryA1), OnExit = nameof(OnExitA1), IsInitial = true)]
//    // [InitialSubstate(HsmState.A, HsmState.A1)] - deprecated, use IsInitial in State attribute
//    void A1() { }

//    [State(HsmState.A2, Parent = HsmState.A, History = HistoryMode.Deep, OnEntry = nameof(OnEntryA2), OnExit = nameof(OnExitA2))]
//    void A2() { }

//    [State(HsmState.A3, Parent = HsmState.A2, OnEntry = nameof(OnEntryA3), OnExit = nameof(OnExitA3), IsInitial = true)]
//    // [InitialSubstate(HsmState.A2, HsmState.A3)] - deprecated, use IsInitial in State attribute
//    void A3() { }

//    [State(HsmState.B, Parent = HsmState.Root, OnEntry = nameof(OnEntryB), OnExit = nameof(OnExitB))]
//    void B() { }

//    [Transition(HsmState.A1, HsmTrigger.Next, HsmState.A3)]
//    void A1ToA3() => ExecutionLog.Add("Action-A1-to-A3");

//    [Transition(HsmState.A, HsmTrigger.ToB, HsmState.B)]
//    void AToB() => ExecutionLog.Add("Action-A-to-B");

//    [Transition(HsmState.B, HsmTrigger.ToA, HsmState.A)]
//    void BToA() => ExecutionLog.Add("Action-B-to-A");

//    void OnEntryA() => ExecutionLog.Add("Entry-A");
//    void OnExitA() => ExecutionLog.Add("Exit-A");
//    void OnEntryA1() => ExecutionLog.Add("Entry-A1");
//    void OnExitA1() => ExecutionLog.Add("Exit-A1");
//    void OnEntryA2() => ExecutionLog.Add("Entry-A2");
//    void OnExitA2() => ExecutionLog.Add("Exit-A2");
//    void OnEntryA3() => ExecutionLog.Add("Entry-A3");
//    void OnExitA3() => ExecutionLog.Add("Exit-A3");
//    void OnEntryB() => ExecutionLog.Add("Entry-B");
//    void OnExitB() => ExecutionLog.Add("Exit-B");
//}

//[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
//public partial class HsmInternalTransitionMachine
//{
//    public List<string> ExecutionLog { get; } = new();

//    [State(HsmState.Root)]
//    void Root() { }

//    [State(HsmState.A, Parent = HsmState.Root, OnEntry = nameof(OnEntryA), OnExit = nameof(OnExitA))]
//    void A() { }

//    [State(HsmState.A1, Parent = HsmState.A, OnEntry = nameof(OnEntryA1), OnExit = nameof(OnExitA1), IsInitial = true)]
//    // [InitialSubstate(HsmState.A, HsmState.A1)] - deprecated, use IsInitial in State attribute
//    void A1() { }

//    [State(HsmState.A2, Parent = HsmState.A, OnEntry = nameof(OnEntryA2), OnExit = nameof(OnExitA2))]
//    void A2() { }

//    // Internal transition defined on parent
//    [InternalTransition(HsmState.A, HsmTrigger.Internal, nameof(AInternalAction))]
//    void AInternal() { }
    
//    void AInternalAction() => ExecutionLog.Add("Action-A-Internal");

//    // External transition for comparison
//    [Transition(HsmState.A1, HsmTrigger.External, HsmState.A1)]
//    void A1External() => ExecutionLog.Add("Action-A1-External");

//    [Transition(HsmState.A1, HsmTrigger.Next, HsmState.A2)]
//    void A1ToA2() => ExecutionLog.Add("Action-A1-to-A2");

//    void OnEntryA() => ExecutionLog.Add("Entry-A");
//    void OnExitA() => ExecutionLog.Add("Exit-A");
//    void OnEntryA1() => ExecutionLog.Add("Entry-A1");
//    void OnExitA1() => ExecutionLog.Add("Exit-A1");
//    void OnEntryA2() => ExecutionLog.Add("Entry-A2");
//    void OnExitA2() => ExecutionLog.Add("Exit-A2");
//}

//[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
//public partial class TransitionInheritanceMachine
//{
//    public List<string> ExecutionLog { get; } = new();

//    [State(HsmState.Root)]
//    void Root() { }

//    [State(HsmState.A, Parent = HsmState.Root)]
//    void A() { }

//    [State(HsmState.A1, Parent = HsmState.A, IsInitial = true)]
//    // [InitialSubstate(HsmState.A, HsmState.A1)] - deprecated, use IsInitial in State attribute
//    void A1() { }

//    [State(HsmState.A2, Parent = HsmState.A)]
//    void A2() { }

//    [State(HsmState.B, Parent = HsmState.Root)]
//    void B() { }

//    [State(HsmState.C, Parent = HsmState.Root)]
//    void C() { }

//    // Transition defined on parent - should work for all children
//    [Transition(HsmState.A, HsmTrigger.ToB, HsmState.B)]
//    void AToB() => ExecutionLog.Add("Action-A-to-B");

//    // Override in child - should take precedence
//    [Transition(HsmState.A2, HsmTrigger.ToB, HsmState.C)]
//    void A2ToC() => ExecutionLog.Add("Action-A2-to-C");

//    // Transition defined on root - should work from any state
//    [Transition(HsmState.Root, HsmTrigger.ToC, HsmState.C)]
//    void RootToC() => ExecutionLog.Add("Action-Root-to-C");
//}

//[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
//public partial class ExceptionHandlingHsmMachine
//{
//    public List<string> ExecutionLog { get; } = new();
//    public bool ThrowInExit { get; set; }
//    public bool ThrowInEntry { get; set; }
//    public bool ThrowInAction { get; set; }

//    [State(HsmState.Root)]
//    void Root() { }

//    [State(HsmState.A, Parent = HsmState.Root, OnEntry = nameof(OnEntryA), OnExit = nameof(OnExitA))]
//    void A() { }

//    [State(HsmState.A1, Parent = HsmState.A, OnEntry = nameof(OnEntryA1), OnExit = nameof(OnExitA1), IsInitial = true)]
//    // [InitialSubstate(HsmState.A, HsmState.A1)] - deprecated, use IsInitial in State attribute
//    void A1() { }

//    [State(HsmState.B, Parent = HsmState.Root, OnEntry = nameof(OnEntryB), OnExit = nameof(OnExitB))]
//    void B() { }

//    [Transition(HsmState.A1, HsmTrigger.ToB, HsmState.B)]
//    void A1ToB()
//    {
//        ExecutionLog.Add("Action-A1-to-B");
//        if (ThrowInAction)
//            throw new InvalidOperationException("Action exception");
//    }

//    ExceptionDirective HandleException(ExceptionContext<HsmState, HsmTrigger> context)
//    {
//        ExecutionLog.Add($"Exception-{context.Stage}-{context.Exception.Message}");
//        return ExceptionDirective.Continue;
//    }

//    void OnEntryA()
//    {
//        ExecutionLog.Add("Entry-A");
//        if (ThrowInEntry)
//            throw new InvalidOperationException("Entry exception");
//    }

//    void OnExitA()
//    {
//        ExecutionLog.Add("Exit-A");
//        if (ThrowInExit)
//            throw new InvalidOperationException("Exit exception");
//    }

//    void OnEntryA1()
//    {
//        ExecutionLog.Add("Entry-A1");
//        if (ThrowInEntry)
//            throw new InvalidOperationException("Entry exception");
//    }

//    void OnExitA1()
//    {
//        ExecutionLog.Add("Exit-A1");
//        if (ThrowInExit)
//            throw new InvalidOperationException("Exit exception");
//    }

//    void OnEntryB()
//    {
//        ExecutionLog.Add("Entry-B");
//        if (ThrowInEntry)
//            throw new InvalidOperationException("Entry exception");
//    }

//    void OnExitB()
//    {
//        ExecutionLog.Add("Exit-B");
//        if (ThrowInExit)
//            throw new InvalidOperationException("Exit exception");
//    }
//}