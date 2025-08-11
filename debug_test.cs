using System;
using StateMachine.Tests.HierarchicalTests;

class DebugTest
{
    static void Main()
    {
        var m = new HsmParsingCompilationTests.SimpleParentChildMachine(HsmParsingCompilationTests.HsmState.Idle);
        m.Start();
        
        Console.WriteLine($"Initial state: {m.CurrentState}");
        
        // Fire the Start trigger to move to Working hierarchy
        m.Fire(HsmParsingCompilationTests.HsmTrigger.Start);
        Console.WriteLine($"After Start trigger: {m.CurrentState}");
        
        // Check IsInHierarchy
        Console.WriteLine($"IsInHierarchy(Working_Initializing): {m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working_Initializing)}");
        Console.WriteLine($"IsInHierarchy(Working): {m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working)}");
        Console.WriteLine($"IsInHierarchy(Idle): {m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Idle)}");
        
        // Debug: print state indices
        Console.WriteLine($"\nState indices:");
        Console.WriteLine($"CurrentState index: {(int)m.CurrentState}");
        Console.WriteLine($"Working_Initializing index: {(int)HsmParsingCompilationTests.HsmState.Working_Initializing}");
        Console.WriteLine($"Working index: {(int)HsmParsingCompilationTests.HsmState.Working}");
    }
}