using Abstractions.Attributes;

namespace StateMachine.Tests.DI.TestMachines;

// Common test enums
public enum TestState { A, B, C, D }
public enum TestTrigger { Next, Back, Reset }

// Test data for payload variants
public class TestData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// === PURE VARIANT ===
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = false)]
public partial class PureTestMachine
{
    [Transition(TestState.A, TestTrigger.Next, TestState.B)]
    [Transition(TestState.B, TestTrigger.Next, TestState.C)]
    [Transition(TestState.C, TestTrigger.Next, TestState.D)]
    [Transition(TestState.D, TestTrigger.Reset, TestState.A)]
    private void Configure() { }
}

// === BASIC VARIANT ===
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = false)]
public partial class BasicTestMachine
{
    public int EnterCount { get; private set; }
    public int ExitCount { get; private set; }
    public TestState? LastEnteredState { get; private set; }
    public TestState? LastExitedState { get; private set; }

    [Transition(TestState.A, TestTrigger.Next, TestState.B)]
    [Transition(TestState.B, TestTrigger.Next, TestState.C)]
    [Transition(TestState.C, TestTrigger.Back, TestState.B)]
    private void Configure() { }

    [State(TestState.A, OnEntry = nameof(EnterA), OnExit = nameof(ExitA))]
    [State(TestState.B, OnEntry = nameof(EnterB), OnExit = nameof(ExitB))]
    [State(TestState.C, OnEntry = nameof(EnterC))]
    private void ConfigureStates() { }

    private void EnterA() { EnterCount++; LastEnteredState = TestState.A; }
    private void ExitA() { ExitCount++; LastExitedState = TestState.A; }
    private void EnterB() { EnterCount++; LastEnteredState = TestState.B; }
    private void ExitB() { ExitCount++; LastExitedState = TestState.B; }
    private void EnterC() { EnterCount++; LastEnteredState = TestState.C; }
}

// === WITH PAYLOAD VARIANT ===
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = false)]
[PayloadType(typeof(TestData))]
public partial class PayloadTestMachine
{
    public TestData? LastProcessedData { get; private set; }
    public bool GuardExecuted { get; private set; }

    [Transition(TestState.A, TestTrigger.Next, TestState.B,
        Guard = nameof(CanProcess), Action = nameof(ProcessData))]
    [Transition(TestState.B, TestTrigger.Next, TestState.C)]
    private void Configure() { }

    private bool CanProcess(TestData data)
    {
        GuardExecuted = true;
        return data.Id > 0;
    }

    private void ProcessData(TestData data)
    {
        LastProcessedData = data;
    }
}

// === WITH EXTENSIONS VARIANT ===
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensionsTestMachine
{
    [Transition(TestState.A, TestTrigger.Next, TestState.B)]
    [Transition(TestState.B, TestTrigger.Next, TestState.C)]
    [Transition(TestState.C, TestTrigger.Back, TestState.B)]
    private void Configure() { }
}

// Extensions variant with guards for testing guard hooks
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
public partial class GuardedTestMachine
{
    [Transition(TestState.A, TestTrigger.Next, TestState.B, Guard = nameof(CanTransition))]
    private void Configure() { }

    private bool CanTransition() => true;
}

// === FULL VARIANT (Payload + Extensions) ===
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
[PayloadType(typeof(TestData))]
public partial class FullTestMachine
{
    public TestData? LastData { get; private set; }
    public int ActionCount { get; private set; }

    [Transition(TestState.A, TestTrigger.Next, TestState.B,
        Guard = nameof(ValidateData), Action = nameof(ProcessWithData))]
    [Transition(TestState.B, TestTrigger.Reset, TestState.A)]
    private void Configure() { }

    [State(TestState.B, OnEntry = nameof(OnEnterB))]
    private void ConfigureStates() { }

    private bool ValidateData(TestData data) => data.Id > 0;

    private void ProcessWithData(TestData data)
    {
        LastData = data;
        ActionCount++;
    }

    private void OnEnterB(TestData data)
    {
        // OnEntry with payload
    }

}

// === MULTI-PAYLOAD VARIANT ===
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = false)]
[PayloadType(TestTrigger.Next, typeof(TestData))]
[PayloadType(TestTrigger.Back, typeof(string))]
public partial class MultiPayloadTestMachine
{
    public object? LastPayload { get; private set; }

    [Transition(TestState.A, TestTrigger.Next, TestState.B, Action = nameof(ProcessNext))]
    [Transition(TestState.B, TestTrigger.Back, TestState.A, Action = nameof(ProcessBack))]
    private void Configure() { }

    private void ProcessNext(TestData data)
    {
        LastPayload = data;
    }

    private void ProcessBack(string message)
    {
        LastPayload = message;
    }
}