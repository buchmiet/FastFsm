using System;
using System.IO;
using System.Linq;
using FastFsm.HsmPrototype.Core;
using FastFsm.HsmPrototype.TestModels;

Console.WriteLine("=== FastFSM HSM Prototype Generator ===\n");

// Create test model
var model = SimpleHierarchy.Create();
Console.WriteLine($"Model: {model.ClassName}");
Console.WriteLine($"States: {string.Join(", ", model.States.Keys)}");
Console.WriteLine($"Transitions: {model.Transitions.Count}");
Console.WriteLine($"Hierarchy: {string.Join(", ", model.ChildrenOf.Select(kv => $"{kv.Key}->[{string.Join(",", kv.Value)}]"))}");

Console.WriteLine("\n=== Hierarchy Info ===");
foreach (var state in model.States.Values)
{
    if (state.ParentState != null)
    {
        Console.WriteLine($"{state.Name} -> Parent: {state.ParentState}");
    }
    if (state.IsInitial)
    {
        Console.WriteLine($"{state.Name} is INITIAL for {state.ParentState}");
    }
}

Console.WriteLine("\n=== Transitions by Priority ===");
var byPriority = model.Transitions
    .OrderByDescending(t => t.Priority)
    .ThenBy(t => t.FromState)
    .ThenBy(t => t.Trigger);

foreach (var t in byPriority)
{
    Console.WriteLine($"[{t.Priority,3}] {t.FromState} + {t.Trigger} -> {t.ToState}" +
                      $"{(t.IsInternal ? " (INTERNAL)" : "")}" +
                      $"{(!string.IsNullOrEmpty(t.GuardMethod) ? $" [guard: {t.GuardMethod}]" : "")}" +
                      $"{(!string.IsNullOrEmpty(t.ActionMethod) ? $" [action: {t.ActionMethod}]" : "")}");
}
Console.WriteLine();

// Generate code
var generator = new HierarchicalCodeGenerator(model);
var generatedCode = generator.Generate();

Console.WriteLine("=== Generated Code ===\n");
Console.WriteLine(generatedCode);

// Save to file
var outputPath = "Output/SimpleHierarchy.generated.cs";
Directory.CreateDirectory("Output");
File.WriteAllText(outputPath, generatedCode);
Console.WriteLine($"\nCode saved to: {outputPath}");

// Test history model
Console.WriteLine("\n=== Testing History Model ===\n");
var historyModel = HistoryHierarchy.Create();
Console.WriteLine($"History Model: {historyModel.ClassName}");
Console.WriteLine($"States with history:");
foreach (var kvp in historyModel.HistoryOf)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
}

var historyGen = new HierarchicalCodeGenerator(historyModel);
var historyCode = historyGen.Generate();

Console.WriteLine("\n=== History Generated Code (excerpt) ===\n");
// Show only first 50 lines
var historyLines = historyCode.Split('\n').Take(50);
foreach (var line in historyLines)
{
    Console.WriteLine(line);
}

File.WriteAllText("Output/HistoryHierarchy.generated.cs", historyCode);
Console.WriteLine($"\nHistory code saved to: Output/HistoryHierarchy.generated.cs");