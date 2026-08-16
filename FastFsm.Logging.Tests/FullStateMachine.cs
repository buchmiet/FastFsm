using Abstractions.Attributes;

namespace FastFsm.Logging.Tests;

[StateMachine(typeof(TestState), typeof(TestTrigger),
    GenerateExtensibleVersion = true, DefaultPayloadType = typeof(TestPayload))]
public partial class FullStateMachine
{
    public TestPayload? LastPayload { get; private set; }
    public bool GuardResult { get; set; } = true;

    [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
        Guard = nameof(CanStart), Action = nameof(ProcessAction))]
    [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
    private void ConfigureWithPayload() { }

    // Guard with payload
    private bool CanStart(TestPayload payload)
    {
        LastPayload = payload;
        return GuardResult;
    }

    // Parameterless guard
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