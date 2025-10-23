using Abstractions.Fluent;
using Abstractions.Attributes;

namespace FastFsm.Logging.Tests;

// Pure variant state machine for testing - Fluent version
[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class PureStateMachineFluent
{
    private static void Configure() => FSM
        .State(TestState.Initial).On(TestTrigger.Start).GoTo(TestState.Processing)
        .State(TestState.Processing).On(TestTrigger.Complete).GoTo(TestState.Completed);
}

// Basic variant with OnEntry/OnExit - Fluent version
[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class BasicStateMachineFluent
{
    public int OnEntryCallCount { get; private set; }
    public int OnExitCallCount { get; private set; }
    public int ActionCallCount { get; private set; }
    public bool GuardResult { get; set; } = true;

    private void Configure() => FSM
        .State(TestState.Initial)
            .OnExit(nameof(OnInitialExit))
            .On(TestTrigger.Start)
                .If(nameof(CanStart))
                .Action(nameof(StartAction))
                .GoTo(TestState.Processing).And()
        .State(TestState.Processing)
            .OnEntry(nameof(OnProcessingEntry));

    private bool CanStart() => GuardResult;
    private void StartAction() => ActionCallCount++;
    private void OnProcessingEntry() => OnEntryCallCount++;
    private void OnInitialExit() => OnExitCallCount++;
}

// WithPayload variant - Fluent version
[StateMachine(typeof(TestState), typeof(TestTrigger), DefaultPayloadType = typeof(TestPayload))]
public partial class PayloadStateMachine
{
    public TestPayload? LastPayload { get; private set; }
    public bool GuardResult { get; set; } = true;

    private static void Configure() => FSM
        .State(TestState.Initial)
            .On(TestTrigger.Start)
                .If(nameof(CanStart))
                .Action(nameof(ProcessAction))
                .GoTo(TestState.Processing).And()
        .State(TestState.Processing)
            .OnEntry(nameof(OnProcessingEntry))
            .On(TestTrigger.Complete).GoTo(TestState.Completed).And()
            .On(TestTrigger.Fail).GoTo(TestState.Failed).And()
        .State(TestState.Completed)
            .On(TestTrigger.Reset).GoTo(TestState.Initial).And()
        .State(TestState.Failed)
            .On(TestTrigger.Reset).GoTo(TestState.Initial);

    private bool CanStart(TestPayload payload)
    {
        LastPayload = payload;
        return GuardResult;
    }

    private bool CanStart() => GuardResult;

    private void ProcessAction(TestPayload payload)
    {
        LastPayload = payload;
    }

    private void ProcessAction() { }

    private void OnProcessingEntry(TestPayload payload)
    {
        LastPayload = payload;
    }

    private void OnProcessingEntry() { }
}

// WithExtensions variant - Fluent version
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensionsStateMachineFluent
{
    public bool GuardResult { get; set; } = true;

    private static void Configure() => FSM
        .State(TestState.Initial)
            .On(TestTrigger.Start)
                .If(nameof(CanStart))
                .Action(nameof(StartAction))
                .GoTo(TestState.Processing).And()
        .State(TestState.Processing)
            .OnEntry(nameof(OnProcessingEntry))
            .On(TestTrigger.Complete).GoTo(TestState.Completed).And()
            .On(TestTrigger.Fail).GoTo(TestState.Failed);

    private bool CanStart() => GuardResult;
    private void StartAction() { }
    private void OnProcessingEntry() { }
}

// Full variant (Payload + Extensions) - Fluent version
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true, DefaultPayloadType = typeof(TestPayload))]
public partial class FullStateMachineFluent
{
    public TestPayload? LastPayload { get; private set; }
    public bool GuardResult { get; set; } = true;

    private static void Configure() => FSM
        .State(TestState.Initial)
            .On(TestTrigger.Start)
                .If(nameof(CanStart))
                .Action(nameof(ProcessAction))
                .GoTo(TestState.Processing).And()
        .State(TestState.Processing)
            .OnEntry(nameof(OnProcessingEntry));

    private bool CanStart(TestPayload payload)
    {
        LastPayload = payload;
        return GuardResult;
    }

    private bool CanStart() => GuardResult;

    private void ProcessAction(TestPayload payload)
    {
        LastPayload = payload;
    }

    private void ProcessAction() { }

    private void OnProcessingEntry(TestPayload payload)
    {
        LastPayload = payload;
    }

    private void OnProcessingEntry() { }
}

// Multi-payload variant for testing payload validation - Fluent version
[StateMachine(typeof(TestState), typeof(TestTrigger))]
[PayloadType(TestTrigger.Start, typeof(TestPayload))]
[PayloadType(TestTrigger.Process, typeof(string))]
public partial class MultiPayloadStateMachineFluent
{
    private static void Configure() => FSM
        .State(TestState.Initial)
            .On(TestTrigger.Start).GoTo(TestState.Processing).And()
            .On(TestTrigger.Process).GoTo(TestState.Processing);
}