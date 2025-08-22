using FsmTest;

Console.WriteLine("Testing FastFSM State Machines");
Console.WriteLine("==============================\n");

// Test Simple FSM
Console.WriteLine("Testing Simple FSM (Door Controller):");
Console.WriteLine("--------------------------------------");
var door = new DoorController(DoorState.Closed);
door.Start();

Console.WriteLine($"Initial state: {door.CurrentState}");
Console.WriteLine($"Can open? {door.CanFire(DoorTrigger.Open)}");

door.Fire(DoorTrigger.Open);
Console.WriteLine($"After opening: {door.CurrentState}");

door.Fire(DoorTrigger.Close);
Console.WriteLine($"After closing: {door.CurrentState}");

door.Fire(DoorTrigger.Lock);
Console.WriteLine($"After locking: {door.CurrentState}");

door.Fire(DoorTrigger.Unlock);
Console.WriteLine($"After unlocking: {door.CurrentState}");

// Test Hierarchical FSM
Console.WriteLine("\nTesting Hierarchical FSM (Workflow Machine):");
Console.WriteLine("---------------------------------------------");
var workflow = new WorkflowMachine(WorkflowState.Idle);
workflow.Start();

Console.WriteLine($"Initial state: {workflow.CurrentState}");

workflow.Fire(WorkflowTrigger.Start);
Console.WriteLine($"After starting: {workflow.CurrentState}");

workflow.Fire(WorkflowTrigger.UpdateProgress);
Console.WriteLine($"After progress update (internal transition): {workflow.CurrentState}");

workflow.Fire(WorkflowTrigger.Next);
Console.WriteLine($"After next: {workflow.CurrentState}");

workflow.Fire(WorkflowTrigger.Next);
Console.WriteLine($"After next: {workflow.CurrentState}");

workflow.Fire(WorkflowTrigger.Finish);
Console.WriteLine($"After finish: {workflow.CurrentState}");

Console.WriteLine("\nAll tests completed successfully!");
