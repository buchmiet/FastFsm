using Abstractions.Attributes;

namespace CleanUp;

public enum HsmSimpleState
{
    Root,
    Working,
    Initializing,
    Running,
    Error
}

public enum HsmSimpleTrigger
{
    Start,
    InitDone,
    Fail,
    Reset
}

[StateMachine(typeof(HsmSimpleState), typeof(HsmSimpleTrigger))]
public partial class HsmSimpleStateMachine
{
    // Hierarchy: Root -> Working (composite) -> { Initializing (initial), Running }, and Error (leaf)
    [State(HsmSimpleState.Root)]
    [State(HsmSimpleState.Working, Parent = HsmSimpleState.Root, IsInitial = true, OnEntry = nameof(OnEnterWorking))]
    [State(HsmSimpleState.Initializing, Parent = HsmSimpleState.Working, IsInitial = true, OnEntry = nameof(OnEnterInitializing))]
    [State(HsmSimpleState.Running, Parent = HsmSimpleState.Working, OnEntry = nameof(OnEnterRunning))]
    [State(HsmSimpleState.Error, Parent = HsmSimpleState.Root, OnEntry = nameof(OnEnterError))]
    private void ConfigureStates() { }

    // Transitions
    [Transition(HsmSimpleState.Initializing, HsmSimpleTrigger.InitDone, HsmSimpleState.Running)]
    [Transition(HsmSimpleState.Initializing, HsmSimpleTrigger.Fail, HsmSimpleState.Error)]
    [Transition(HsmSimpleState.Running, HsmSimpleTrigger.Fail, HsmSimpleState.Error)]
    [Transition(HsmSimpleState.Error, HsmSimpleTrigger.Reset, HsmSimpleState.Working)] // to composite → resolves to Initializing
    private void ConfigureTransitions() { }

    private void OnEnterWorking() { }
    private void OnEnterInitializing() { }
    private void OnEnterRunning() { }
    private void OnEnterError() { }
}
