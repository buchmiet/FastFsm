using System;
using Abstractions.Attributes;

namespace Troubleshot;

public enum SimpleState { A, B }
public enum SimpleTrigger { Next }

[StateMachine(typeof(SimpleState), typeof(SimpleTrigger))]
public partial class SimpleMachine
{
    [State(SimpleState.A)]
    [State(SimpleState.B)]
    private void ConfigureStates() { }

    [Transition(SimpleState.A, SimpleTrigger.Next, SimpleState.B)]
    private void ConfigureTransitions() { }
}

internal class Program
{
    private static void Main()
    {
        var m = new SimpleMachine(SimpleState.A);
        m.Start();
        Console.WriteLine($"Start: {m.CurrentState}");
        var ok = m.TryFire(SimpleTrigger.Next);
        Console.WriteLine($"Fired Next: {ok}, state={m.CurrentState}");
    }
}

