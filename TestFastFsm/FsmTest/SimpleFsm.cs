using Abstractions.Attributes;

namespace FsmTest;

// Simple FSM example from README
public enum DoorState { Open, Closed, Locked }
public enum DoorTrigger { Open, Close, Lock, Unlock }

[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    [Transition(DoorState.Closed, DoorTrigger.Open, DoorState.Open)]
    [Transition(DoorState.Open, DoorTrigger.Close, DoorState.Closed)]
    [Transition(DoorState.Closed, DoorTrigger.Lock, DoorState.Locked)]
    [Transition(DoorState.Locked, DoorTrigger.Unlock, DoorState.Closed)]
    private void ConfigureTransitions() { }

    [State(DoorState.Open, OnEntry = nameof(OnDoorOpened))]
    private void ConfigureOpen() { }
    
    private void OnDoorOpened() => Console.WriteLine("Door opened!");
}