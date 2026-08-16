using Abstractions.Attributes;

namespace Example.Tests;

public enum LightState
{
    Off,
    On
}

public enum LightTrigger
{
    TurnOn,
    TurnOff
}

[StateMachine(typeof(LightState), typeof(LightTrigger))]
public partial class LightMachine
{
    [State(LightState.Off, IsInitial = true)]
    [State(LightState.On)]
    [Transition(LightState.Off, LightTrigger.TurnOn, LightState.On)]
    [Transition(LightState.On, LightTrigger.TurnOff, LightState.Off)]
    private void Configure()
    {
    }
}

public class ExampleTests
{
    [Fact]
    public void LightMachine_Fire_Toggles_State()
    {
        var machine = new LightMachine(LightState.Off);

        machine.Start();
        Assert.Equal(LightState.Off, machine.CurrentState);

        machine.Fire(LightTrigger.TurnOn);
        Assert.Equal(LightState.On, machine.CurrentState);

        Assert.True(machine.TryFire(LightTrigger.TurnOff));
        Assert.Equal(LightState.Off, machine.CurrentState);
    }
}
