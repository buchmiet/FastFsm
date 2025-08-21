using Example.TestWarnings;

Console.WriteLine("Testing FSM004 warnings...");

var proper = new ProperStateMachine(States.Idle);
proper.Start();
Console.WriteLine($"Current state: {proper.CurrentState}");

Console.WriteLine("Done!");