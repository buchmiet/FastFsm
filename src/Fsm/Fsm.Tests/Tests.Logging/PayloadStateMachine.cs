using Abstractions.Attributes;

namespace Tests.Logging;

[StateMachine(typeof(TestState), typeof(TestTrigger), DefaultPayloadType = typeof(TestPayload))]
public partial class PayloadStateMachine
{
    public TestPayload? LastPayload { get; private set; }
    public bool GuardResult { get; set; } = true;

    [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
        Guard = nameof(CanStart), Action = nameof(ProcessAction))]
    [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
    private void ConfigureWithPayload() { }

    [Transition(TestState.Processing, TestTrigger.Complete, TestState.Completed)]
    [Transition(TestState.Processing, TestTrigger.Fail, TestState.Failed)]
    private void ConfigureOthers() { }

    [Transition(TestState.Processing, TestTrigger.Complete, TestState.Completed)]
    [Transition(TestState.Processing, TestTrigger.Fail, TestState.Failed)]
    [Transition(TestState.Completed, TestTrigger.Reset, TestState.Initial)]
    [Transition(TestState.Failed, TestTrigger.Reset, TestState.Initial)]
    private void ConfigureComplete() { }

    // Guard with payload overload
    private bool CanStart(TestPayload payload)
    {
        LastPayload = payload;
        return GuardResult;
    }

    // Parameterless guard overload
    private bool CanStart() => GuardResult;

    // Action with payload
    private void ProcessAction(TestPayload payload)
    {
        LastPayload = payload;
    }

    // Parameterless action
    private void ProcessAction() { }

    // OnEntry with payload
    private void OnProcessingEntry(TestPayload payload)
    {
        LastPayload = payload;
    }

    // Parameterless OnEntry
    private void OnProcessingEntry() { }
}