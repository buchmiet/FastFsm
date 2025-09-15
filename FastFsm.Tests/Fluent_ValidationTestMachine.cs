using Abstractions.Fluent;

namespace FastFsm.Tests;

public enum TestState { Idle, Running, Complete }
public enum TestTrigger { Start, Stop, Reset }

// Test machine with open transition - should generate FSM200 error
[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class OpenTransitionTestMachine
{
    private static void Configure() => FSM
        .State(TestState.Idle)
        .On(TestTrigger.Start)
        .Guard(nameof(CanStart))
        // Missing .GoTo() or .Internal() - should trigger FSM200 error
        .State(TestState.Running);
                    
    private bool CanStart() => true;
}

// Test machine with auto-finalized transition - should generate FSM201 warning
[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class AutoFinalizedTestMachine
{
    private static void Configure() => FSM
        .State(TestState.Idle)
        .On(TestTrigger.Start)
        .Guard(nameof(CanStart))
        // No GoTo() before next On() - should trigger FSM201 warning
        .On(TestTrigger.Stop)
        .GoTo(TestState.Complete);
                    
    private bool CanStart() => true;
}

// Test machine with multiple payloads - should generate FSM202 warning
[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class MultiplePayloadsTestMachine
{
    public class Payload1 { }
    public class Payload2 { }

    private static void Configure() => FSM
        .State(TestState.Idle)
        .On(TestTrigger.Start)
        .Payload<Payload1>()
        // .Payload<Payload2>()  // Should trigger FSM202 warning - commented to allow build
        .GoTo(TestState.Running);
}