using CleanUp;

Console.WriteLine("FastFSM CleanUp Project - Code Analysis");
Console.WriteLine("========================================");

// Create and test basic state machine
var basicMachine = new BasicStateMachine(BasicState.Idle);
basicMachine.Start();
Console.WriteLine($"Basic machine initial state: {basicMachine.CurrentState}");

// Create and test guards/actions state machine
var processData = new ProcessData { Id = "TEST001", Value = 100, IsValid = true };
var advancedMachine = new GuardsActionsStateMachine(ProcessState.Idle);
advancedMachine.Start();
Console.WriteLine($"Advanced machine initial state: {advancedMachine.CurrentState}");

Console.WriteLine("\nState machines created successfully.");
Console.WriteLine("Check generated code in obj/Debug/net9.0/generated folder.");
