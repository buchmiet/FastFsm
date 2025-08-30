using Abstractions.Attributes;

// Simple FSM with transitions
[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorMachine 
{
    [Transition(DoorState.Closed, DoorTrigger.Open, DoorState.Opened)]
    [Transition(DoorState.Opened, DoorTrigger.Close, DoorState.Closed)]
    [Transition(DoorState.Closed, DoorTrigger.Lock, DoorState.Locked)]
    [Transition(DoorState.Locked, DoorTrigger.Unlock, DoorState.Closed)]
    private void ConfigureTransitions() { }
    
    [InternalTransition(DoorState.Opened, DoorTrigger.Knock, Action = "HandleKnock")]
    private void ConfigureInternal() { }
    
    private void HandleKnock() 
    {
        // Internal action
    }
}

public enum DoorState 
{ 
    Closed, 
    Opened, 
    Locked 
}

public enum DoorTrigger 
{ 
    Open, 
    Close, 
    Lock, 
    Unlock,
    Knock
}