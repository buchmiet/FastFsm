using System.Threading.Tasks;
using Abstractions.Attributes;

namespace ParserComparison.Tests;

// Test async + extensions with attributes (should work as reference)
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
public partial class AsyncExtensionAttributeTest
{
    [State(TestState.Idle, OnEntry = nameof(OnEnterIdleAsync))]
    [State(TestState.Working)]
    private void ConfigureStates() { }
    
    [Transition(TestState.Idle, TestTrigger.Start, TestState.Working, Action = nameof(DoWorkAsync))]
    private void ConfigureTransitions() { }
    
    private async Task OnEnterIdleAsync()
    {
        await Task.Yield();
    }
    
    private async Task DoWorkAsync()
    {
        await Task.Yield();
    }
}

public enum TestState { Idle, Working }
public enum TestTrigger { Start }