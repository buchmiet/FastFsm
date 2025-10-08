using Demo;

var machine = new SimpleMachine(State.Idle);
machine.Start();
Console.WriteLine($"Current state after Start(): {machine.CurrentState}");

machine.Fire(Trigger.Start);
Console.WriteLine($"Current state after firing Start: {machine.CurrentState}");
