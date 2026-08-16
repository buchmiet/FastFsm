using Abstractions.Attributes;

namespace Example.GeneratorDirect.Tests;

public enum SimpleState
{
    Idle,
    Active
}

public enum SimpleTrigger
{
    Start,
    Stop
}

[StateMachine(typeof(SimpleState), typeof(SimpleTrigger))]
public partial class SimpleMachine
{
    [State(SimpleState.Idle, IsInitial = true)]
    [State(SimpleState.Active)]
    [Transition(SimpleState.Idle, SimpleTrigger.Start, SimpleState.Active)]
    [Transition(SimpleState.Active, SimpleTrigger.Stop, SimpleState.Idle)]
    private void Configure()
    {
    }
}

public class GeneratorDirectTests
{
    [Fact]
    public void SimpleMachine_Fires_Triggers()
    {
        var machine = new SimpleMachine(SimpleState.Idle);

        machine.Start();
        Assert.Equal(SimpleState.Idle, machine.CurrentState);

        machine.Fire(SimpleTrigger.Start);
        Assert.Equal(SimpleState.Active, machine.CurrentState);

        machine.Fire(SimpleTrigger.Stop);
        Assert.Equal(SimpleState.Idle, machine.CurrentState);
    }
}
