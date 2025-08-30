using Abstractions.Attributes;
using System;

namespace TestDoor
{
    // 1. Define your states and triggers as enums
    public enum DoorState   { Open, Closed, Locked }
    public enum DoorTrigger { Open, Close, Lock, Unlock }

    // 2. Declare your state machine class with the [StateMachine] attribute
    [StateMachine(typeof(DoorState), typeof(DoorTrigger))]
    public partial class DoorController
    {
        // 3. Define transitions using attributes on a dummy method
        [Transition(DoorState.Closed, DoorTrigger.Open,   DoorState.Open)]
        [Transition(DoorState.Open,   DoorTrigger.Close,  DoorState.Closed)]
        [Transition(DoorState.Closed, DoorTrigger.Lock,   DoorState.Locked)]
        [Transition(DoorState.Locked, DoorTrigger.Unlock, DoorState.Closed)]
        private void ConfigureTransitions() { }

        // (Optional) Define state entry/exit behaviors:
        [State(DoorState.Open, OnEntry = nameof(OnDoorOpened))]
        private void ConfigureOpen() { }
        
        private void OnDoorOpened() => Console.WriteLine("Door opened!");
    }
}
