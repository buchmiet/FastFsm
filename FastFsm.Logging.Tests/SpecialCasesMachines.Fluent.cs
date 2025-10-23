using Abstractions.Fluent;
using Abstractions.Attributes;

namespace FastFsm.Logging.Tests;

// Internal transition machine - Fluent version
[StateMachine(typeof(InternalState), typeof(InternalTrigger))]
public partial class InternalTransitionMachine
{
    public int RefreshCount { get; private set; }

    private void Configure() => FSM
        .State(InternalState.Active)
            .On(InternalTrigger.Refresh)
                .Action(nameof(DoRefresh))
                .Internal();

    private void DoRefresh() => RefreshCount++;
}

// Struct-based state machine - Fluent version  