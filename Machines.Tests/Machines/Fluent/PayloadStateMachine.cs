#nullable enable
using Abstractions.Fluent;
using Machines.Tests.Machines.Legacy;

namespace Machines.Tests.Machines.Fluent;

// WithPayload variant
[StateMachine(typeof(TestState), typeof(TestTrigger), DefaultPayloadType = typeof(TestPayload))]
public partial class PayloadStateMachine
{
    public TestPayload? LastPayload { get; private set; }
    public bool GuardResult { get; set; } = true;

    private void Configure() => FSM
        .State<TestState>(TestState.Initial)
        .On(TestTrigger.Start).Guard((CanStart)).Action((ProcessAction)).GoTo(TestState.Processing)
        .State(TestState.Processing)
        .OnEntry((OnProcessingEntry))
        .On(TestTrigger.Complete).GoTo(TestState.Completed)
        .On(TestTrigger.Fail).GoTo(TestState.Failed)
        .State(TestState.Completed)
        .On(TestTrigger.Reset).GoTo(TestState.Initial)
        .State(TestState.Failed)
        .On(TestTrigger.Reset).GoTo(TestState.Initial);

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