using Abstractions.Attributes;

namespace CleanUp;

public enum PGState { Idle, Valid, Error }
public enum PGTrigger { Validate, Reset }

public sealed class ValidationData { public int X { get; init; } }

[StateMachine(typeof(PGState), typeof(PGTrigger))]
[PayloadType(typeof(ValidationData))]
public partial class PayloadGuardsStateMachine
{
    // States with callbacks
    [State(PGState.Valid, OnEntry = nameof(OnEnterValid))]
    private void ConfigureStates() { }

    // Transitions
    [Transition(PGState.Idle, PGTrigger.Validate, PGState.Valid, Guard = nameof(IsValid))]
    [Transition(PGState.Valid, PGTrigger.Reset, PGState.Idle)]
    [Transition(PGState.Error, PGTrigger.Reset, PGState.Idle)]
    private void ConfigureTransitions() { }

    private bool IsValid(ValidationData data) => data.X >= 0;
    private void OnEnterValid() { }
}

