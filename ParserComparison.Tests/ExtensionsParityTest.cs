using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

// Legacy API with extensions (reference implementation)
[StateMachine(typeof(ExtState), typeof(ExtTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensionsLegacyMachine
{
    [State(ExtState.Idle, OnEntry = nameof(OnEnterIdle))]
    [State(ExtState.Working, OnExit = nameof(OnExitWorking))]
    [State(ExtState.Complete)]
    private void ConfigureStates() { }
    
    [Transition(ExtState.Idle, ExtTrigger.Start, ExtState.Working, 
        Guard = nameof(CanStart), Action = nameof(StartWork))]
    [Transition(ExtState.Working, ExtTrigger.Finish, ExtState.Complete)]
    [Transition(ExtState.Complete, ExtTrigger.Cancel, ExtState.Idle)]
    private void ConfigureTransitions() { }
    
    private bool CanStart() => true;
    private void StartWork() { }
    private void OnEnterIdle() { }
    private void OnExitWorking() { }
}

// Fluent API with extensions (should generate same model)
[StateMachine(typeof(ExtState), typeof(ExtTrigger))]
public partial class ExtensionsFluentMachine
{
    private static void Configure() => FSM
        .Extensible<ExtState>()
        .State(ExtState.Idle)
            .OnEntry(nameof(OnEnterIdle))
            .On(ExtTrigger.Start).GoTo(ExtState.Working)
                .Guard(nameof(CanStart))
                .Action(nameof(StartWork))
        .State(ExtState.Working)
            .OnExit(nameof(OnExitWorking))
            .On(ExtTrigger.Finish).GoTo(ExtState.Complete)
        .State(ExtState.Complete)
            .On(ExtTrigger.Cancel).GoTo(ExtState.Idle);
    
    // Methods referenced in fluent configuration
    private bool CanStart() => true;
    private void StartWork() { }
    private void OnEnterIdle() { }
    private void OnExitWorking() { }
}

// Fluent API without extensions (should NOT have extensions support)
[StateMachine(typeof(ExtState), typeof(ExtTrigger))]
public partial class NoExtensionsFluentMachine
{
    private static void Configure() => FSM
        // Note: NO .Extensible() call
        .State<ExtState>(ExtState.Idle)
            .OnEntry(nameof(OnEnterIdle))
            .On(ExtTrigger.Start).GoTo(ExtState.Working)
                .Guard(nameof(CanStart))
                .Action(nameof(StartWork))
        .State(ExtState.Working)
            .OnExit(nameof(OnExitWorking))
            .On(ExtTrigger.Finish).GoTo(ExtState.Complete)
        .State(ExtState.Complete)
            .On(ExtTrigger.Cancel).GoTo(ExtState.Idle);
    
    private bool CanStart() => true;
    private void StartWork() { }
    private void OnEnterIdle() { }
    private void OnExitWorking() { }
}

// Test duplicate .Extensible() - should generate warning
[StateMachine(typeof(ExtState), typeof(ExtTrigger))]  
public partial class DuplicateExtensibleMachine
{
    private static void Configure() => FSM
        .Extensible<ExtState>()
        .Extensible<ExtState>()  // Duplicate - should warn
        .State(ExtState.Idle)
            .On(ExtTrigger.Start).GoTo(ExtState.Working);
}

public enum ExtState { Idle, Working, Complete }
public enum ExtTrigger { Start, Finish, Cancel }