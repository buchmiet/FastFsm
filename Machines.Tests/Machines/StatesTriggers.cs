// Auto-generated file containing all State and Trigger enums
// This file is auto-generated. Do not edit manually.

namespace Machines.Tests.Machines;

public enum ASState { A, B }

public enum CSState { A, B }

public enum ConditionalState { Ready, Done }

public enum EP_State { A, B, C }

public enum EmptyState { Only }

public enum ExitState { A, B }

public enum ExtState { Idle, Working, Complete }

public enum HP_State { Root, ChildA, ChildB }

public enum HsmState
{
    // Root states
    Idle,
    Working,
    Completed,
    Error,
    Paused,

    // Working substates (2nd level)
    Working_Initializing,
    Working_Processing,
    Working_Validating,
    Working_Cleanup,

    // Working_Processing substates (3rd level)
    Working_Processing_Reading,
    Working_Processing_Computing,
    Working_Processing_Writing,

    // Working_Processing_Computing substates (4th level - deep hierarchy)
    Working_Processing_Computing_Loading,
    Working_Processing_Computing_Calculating,
    Working_Processing_Computing_Storing,

    // History test states - with proper children
    HistoryParent,
    HistoryParent_Child1,
    HistoryParent_Child2,
    HistoryParent_Child3,

    // Deep history parent with nested children
    DeepHistoryParent,
    DeepHistoryParent_Child1,
    DeepHistoryParent_Child1_SubChild1,
    DeepHistoryParent_Child1_SubChild2,
    DeepHistoryParent_Child2,

    // Priority test states
    Priority_Low,
    Priority_Medium,
    Priority_High,

    // Internal transition test states
    InternalParent,
    InternalParent_Child1,
    InternalParent_Child2,

    // Cross-hierarchy test states
    Branch1,
    Branch1_Leaf1,
    Branch1_Leaf2,
    Branch2,
    Branch2_Leaf1,
    Branch2_Leaf2,

    // Complex scenario states
    ComplexParent,
    ComplexParent_Child1,
    ComplexParent_Child2,
    ComplexParent_Child3,

    // Edge case states
    EdgeParent,
    EdgeParent_Child,

    // Edge case complex states (unique to avoid conflicts)
    EdgeComplexParent,
    EdgeComplexParent_Child1,
    EdgeComplexParent_Child2
}

public enum HsmTrigger
{
    Start,
    Process,
    Complete,
    Validate,
    Execute,
    Pause,
    Resume,
    Reset,
    Initialize,
    Activate,
    Deactivate,
    Submit,
    Approve,
    Reject,
    Timeout,
    Error,
    Recover,
    InternalUpdate,
    InternalProcess,
    MoveNext,
    MovePrevious,
    CrossBranch,
    Abort,
    Finish,
    Cancel,
    Retry,
    Skip
}

public enum InitialPayloadState { Start, Next }

public enum InternalPayloadState { Active, Inactive }

public enum MixedState { Start, Middle, End }

public enum MultiState { Initial, Configured, Processing, Failed }

public enum NH_State { S1, S2 }

public enum NotificationState { Ready, Sent, Failed }

public enum OrderState { New, Submitted, Processing, Completed, Paid, Cancelled, Shipped,Delivered }

public enum OrderTrigger { Submit, Process, Pay, Ship, Deliver, Cancel, Refund }

public enum OverloadState { A, B }

public enum PSState { A, B }

public enum PaymentState { Pending, Processed, Failed }

public enum PermittedState { A, B, C }

public enum ProcessingState { Idle, Running, Completed }

public enum State { Initial, Final }

public enum Trigger { Next }

public enum GuardPermittedState { Idle, Done }

public enum GuardPermittedTrigger { Run }

public enum HookOrderState { A, B }

public enum HookOrderTrigger { Next }

public enum MultipleCallbacksState { A, B }

public enum MultipleCallbacksTrigger { Go }

public enum StrictState { Ready, Processing }

public enum TestTrigger
{
    Go,
    Start,
    Process,
    Complete,
    Fail,
    Reset
}

public enum WorkflowState { Created, Initialized, Submitted, Approved, Completed }

public enum TestState
{
    Initial,
    Processing,
    Completed,
    Failed
}

public enum MultiplePayloadsTestMachine_TestState { Idle, Running, Complete }

public enum AutoFinalizedTestMachine_TestState { Idle, Running, Complete }

public enum ThrowingActionMachine_TestState { A, B }

public enum MultiTrigger { Configure, Process, Error }

public enum ASTrigger { Go }

public enum BenchmarkState { A, B, C, D }

public enum BenchmarkTrigger { Previous, Next }

public enum CSTrigger { Go }

public enum CallbackState { A, B, C }

public enum CallbackTrigger { Next }

public enum CaseSensitiveState
{state,
            State,
            STATE}

public enum CaseSensitiveTrigger
{go,
            Go,
            GO}

public enum ComplexCallbackState { Idle, Ready, Processing, Done }

public enum ComplexCallbackTrigger { Start, Process, Complete }

public enum ConditionalTrigger { Execute }

public enum ConflictState { A, B }

public enum ConflictTrigger { Go }

public enum EDState { A, B }

public enum EDTrigger { Go }

public enum EP_Trigger { Go }

public enum EmptyTrigger { Trigger }

public enum ExceptionState { A, B }

public enum ExceptionTrigger { Go }

public enum ExitTrigger { Go }

public enum ExtTrigger { Start, Finish, Cancel }

public enum GuardedState { A, B }

public enum GuardedTrigger { Go }

public enum HP_Trigger { Configure, Submit }

public enum InitialPayloadTrigger { Go }

public enum InitialState { Start, Next }

public enum InitialTrigger { Go }

public enum InternalOnlyState { Static }

public enum InternalOnlyTrigger { Action }

public enum InternalPayloadTrigger { Update, Deactivate }

public enum InternalState { Active, Inactive }

public enum InternalTrigger { Update, Deactivate }

public enum KeywordState
{@class,
            @return,
            @void,
            @int,
            @interface,
            @namespace}

public enum KeywordTrigger
{@goto,
            @continue,
            @break,
            @new,
            @throw}

public enum LongNameState
{ThisIsAnExtremelyLongStateNameThatShouldStillWorkCorrectlyInTheGeneratedCode_Part1_Part2_Part3_Part4_Part5,
            AnotherVeryLongStateNameForTesting_PartA_PartB_PartC_PartD_PartE_PartF}

public enum LongNameTrigger { ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3 }

public enum MixedTrigger { Regular, Special }

public enum NH_Trigger { Next }

public enum NotificationTrigger { Send, Retry }

public enum NumericState
{_1Start,
            _3Middle,
            _5End}

public enum NumericTrigger
{_2Next,
            _4Continue}

public enum OverloadTrigger { Go }

public enum PSTrigger { Go }

public enum PaymentTrigger { Process, Retry, Cancel }

public enum PermittedTrigger { Next, Skip }

public enum ProcessingTrigger { Start, Stop }

public enum SelfState { Active }

public enum SelfTrigger { Refresh }

public enum SingleState { Only }

public enum SingleTrigger { Loop }

public enum StrictTrigger { Process }

public enum UnicodeState
{αlpha,
            βeta,
            Ωmega}

public enum UnicodeTrigger
{αlpha,
            βeta,
            γamma}

public enum UnreachableState { Start, Connected, Isolated }

public enum UnreachableTrigger { Connect, Disconnect, Isolate }

public enum WorkflowTrigger { Initialize, Submit, Approve, Complete }

// ===== Hierarchical Runtime Machine Enums =====

public enum InitialChildMachine_S { Outside, Parent, Parent_A, Parent_B }

public enum InitialChildMachine_T { EnterParent, Switch, LeaveParent }

public enum ShallowHistoryMachine_S { Outside, Menu, Menu_Main, Menu_Settings }

public enum ShallowHistoryMachine_T { Enter, Next, Back, Exit }

public enum DeepHistoryMachine_S { Out, Work, Work_S1, Work_S1_Loading, Work_S1_Calc }

public enum DeepHistoryMachine_T { EnterWork, Next, Abort }

public enum InternalMachine_S { Parent, Child }

public enum InternalMachine_T { Refresh }

public enum PriorityMachine_S { Parent, Child, ParentDone }

public enum PriorityMachine_T { Go }

public enum ChildOverridesMachine_S { Parent, Child }

public enum ChildOverridesMachine_T { Go }

public enum SourceOrderTieMachine_S { A, B, C }

public enum SourceOrderTieMachine_T { Go }

public enum InheritanceMachine_S { Outside, Parent, Parent_A, Parent_B }

public enum InheritanceMachine_T { Enter, Next, Leave }


public enum PhysicalOrderState { New, Processing, Paid, Shipped, Delivered, Cancelled }
public enum PhysicalOrderTrigger { Process, Pay, Ship, Deliver, Cancel, Refund }

public enum HState { A, A1, A2, B, B1 }
public enum HTrigger { Refresh, MoveToA2, Switch, Back }

public enum TestInitialState { Ready, Working, Done }
public enum TestInitialTrigger { Go, Stop }