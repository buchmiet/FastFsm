using System;
using Abstractions.Attributes;

namespace Test;

public enum State { A, B }
public enum Trigger { Go }

[StateMachine(typeof(State), typeof(Trigger))]
public partial class SimpleMachine
{
    [Transition(State.A, Trigger.Go, State.B)]
    private void Configure() { }
}

class Program
{
    static void Main()
    {
        var m = new SimpleMachine(State.A);
        m.Start();
        m.Fire(Trigger.Go);
        Console.WriteLine($"State: {m.CurrentState}");
    }
}