using Abstractions.Attributes;

namespace CleanUp;

public enum HpgState
{
    Root,
    Idle,
    Validating,
    Active,
    Error
}

public enum HpgTrigger
{
    Start,
    Approve,
    Fail,
    Reset
}

public sealed class Command
{
    public string User { get; init; } = string.Empty;
    public bool Approved { get; init; }
}

[StateMachine(typeof(HpgState), typeof(HpgTrigger))]
[PayloadType(typeof(Command))]
public partial class HsmPayloadGuardsStateMachine
{
    // Hierarchy: Root -> { Idle (initial), Validating, Active, Error }
    [State(HpgState.Root)]
    [State(HpgState.Idle, Parent = HpgState.Root, IsInitial = true, OnEntry = nameof(OnEnterIdle))]
    [State(HpgState.Validating, Parent = HpgState.Root, OnEntry = nameof(OnEnterValidating))]
    [State(HpgState.Active, Parent = HpgState.Root, OnEntry = nameof(OnEnterActive))]
    [State(HpgState.Error, Parent = HpgState.Root, OnEntry = nameof(OnEnterError))]
    private void ConfigureStates() { }

    // Transitions with payload guards
    [Transition(HpgState.Idle, HpgTrigger.Start, HpgState.Validating, Guard = nameof(CanStart))]
    [Transition(HpgState.Validating, HpgTrigger.Approve, HpgState.Active, Guard = nameof(IsApproved))]
    [Transition(HpgState.Validating, HpgTrigger.Fail, HpgState.Error)]
    [Transition(HpgState.Active, HpgTrigger.Fail, HpgState.Error)]
    [Transition(HpgState.Error, HpgTrigger.Reset, HpgState.Idle)]
    private void ConfigureTransitions() { }

    // Guards with payload
    private bool CanStart(Command cmd) => cmd is not null && !string.IsNullOrWhiteSpace(cmd.User);
    private bool IsApproved(Command cmd) => cmd is not null && cmd.Approved;

    // OnEntry callbacks
    private void OnEnterIdle() { }
    private void OnEnterValidating() { }
    private void OnEnterActive() { }
    private void OnEnterError() { }
}
