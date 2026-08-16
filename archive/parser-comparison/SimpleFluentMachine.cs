using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

// This machine uses Fluent API to define the same state machine as SimpleStateMachine
[StateMachine(typeof(State), typeof(Trigger))]
public partial class SimpleFluentMachine
{
    private static void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Start).GoTo(State.Processing)
        .State(State.Processing)
            .On(Trigger.Process).Action(nameof(OnProcess))  // Internal transition
        .State(State.Processing)
            .On(Trigger.Complete).GoTo(State.Completed)
        .State(State.Completed)
            .On(Trigger.Reset).GoTo(State.Idle);
    
    private void OnProcess()
    {
        Console.WriteLine("Processing...");
    }
}