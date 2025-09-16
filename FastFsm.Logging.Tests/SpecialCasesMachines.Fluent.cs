using Abstractions.Fluent;
using Abstractions.Attributes;

namespace FastFsm.Logging.Tests;

// Internal transition machine - Fluent version
[StateMachine(typeof(InternalState), typeof(InternalTrigger))]
public partial class InternalTransitionMachineFluent
{
    public int RefreshCount { get; private set; }

    private static void Configure() => FSM
        .State(InternalState.Active)
            .On(InternalTrigger.Refresh)
                .Action(nameof(DoRefresh))
                .Internal();

    private void DoRefresh() => RefreshCount++;
}

// Struct-based state machine - Fluent version  
[StateMachine(typeof(StructState), typeof(StructTrigger))]
public partial class StructStateMachineFluent
{
    private static void Configure() => FSM
        .State(StructState.One).On(StructTrigger.Next).GoTo(StructState.Two).And()
        .State(StructState.Two).On(StructTrigger.Next).GoTo(StructState.Three);
}
