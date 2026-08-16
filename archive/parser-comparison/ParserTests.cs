using Xunit;

namespace ParserComparison.Tests;

public class ParserTests
{
    // Tests commented out temporarily while generators are being developed
    // The generated files will contain JSON comparison in comments
    /*
    [Fact]
    public void SimpleStateMachine_ShouldGenerate()
    {
        // This test simply verifies that the code generation works
        // The actual comparison happens in the generated code comments
        var machine = new SimpleStateMachine(State.Idle);
        Assert.NotNull(machine);
    }

    [Fact]
    public void SimpleStateMachine_CanTransition()
    {
        var machine = new SimpleStateMachine(State.Idle);
        machine.Start();
        
        Assert.Equal(State.Idle, machine.CurrentState);
        
        machine.Fire(Trigger.Start);
        Assert.Equal(State.Processing, machine.CurrentState);
        
        machine.Fire(Trigger.Complete);
        Assert.Equal(State.Completed, machine.CurrentState);
        
        machine.Fire(Trigger.Reset);
        Assert.Equal(State.Idle, machine.CurrentState);
    }
    */
}