using Abstractions.Attributes;

namespace ParserComparison.Tests;

public enum Trigger
{
    Start,
    Process,
    Complete,
    Reset
}

public enum State
{
    Idle,
    Processing,
    Completed
}

[StateMachine(typeof(State), typeof(Trigger))]
public partial class SimpleStateMachine
{
    [State(State.Idle)]
    private void Idle()
    {
    }

    [State(State.Processing)]
    private void Processing()
    {
    }

    [State(State.Completed)]
    private void Completed()
    {
    }

    [Transition(State.Idle, Trigger.Start, State.Processing)]
    private void OnStart()
    {
        Console.WriteLine("Starting processing...");
    }

    [InternalTransition(State.Processing, Trigger.Process, nameof(OnProcess))]
    private void OnProcess()
    {
        Console.WriteLine("Processing...");
    }

    [Transition(State.Processing, Trigger.Complete, State.Completed)]
    private void OnComplete()
    {
        Console.WriteLine("Processing completed!");
    }

    [Transition(State.Completed, Trigger.Reset, State.Idle)]
    private void OnReset()
    {
        Console.WriteLine("Resetting to idle...");
    }
}