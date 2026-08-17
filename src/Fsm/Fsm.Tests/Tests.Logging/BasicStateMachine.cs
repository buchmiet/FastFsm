using Abstractions.Attributes;

namespace Tests.Logging;

[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class BasicStateMachine
{
    public int OnEntryCallCount { get; private set; }
    public int OnExitCallCount { get; private set; }
    public int ActionCallCount { get; private set; }
    public bool GuardResult { get; set; } = true;

    [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
        Guard = nameof(CanStart), Action = nameof(StartAction))]
    [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
    [State(TestState.Initial, OnExit = nameof(OnInitialExit))]
    private void ConfigureStart() { }

    private bool CanStart() => GuardResult;
    private void StartAction() => ActionCallCount++;
    private void OnProcessingEntry() => OnEntryCallCount++;
    private void OnInitialExit() => OnExitCallCount++;
}