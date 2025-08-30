using Abstractions.Attributes;

// Hierarchical State Machine
[StateMachine(typeof(PhoneState), typeof(PhoneTrigger))]
public partial class PhoneMachine 
{
    // Parent states
    [State(PhoneState.On, OnEntry = "EnterOn", OnExit = "ExitOn")]
    [State(PhoneState.Off)]
    
    // Child states of On
    [State(PhoneState.Idle, Parent = PhoneState.On, IsInitial = true)]
    [State(PhoneState.Calling, Parent = PhoneState.On)]
    [State(PhoneState.InCall, Parent = PhoneState.On)]
    
    // Transitions
    [Transition(PhoneState.Off, PhoneTrigger.PowerOn, PhoneState.On)]
    [Transition(PhoneState.On, PhoneTrigger.PowerOff, PhoneState.Off)]
    [Transition(PhoneState.Idle, PhoneTrigger.Dial, PhoneState.Calling)]
    [Transition(PhoneState.Calling, PhoneTrigger.Connect, PhoneState.InCall)]
    [Transition(PhoneState.InCall, PhoneTrigger.Hangup, PhoneState.Idle)]
    [Transition(PhoneState.Calling, PhoneTrigger.Cancel, PhoneState.Idle)]
    private void ConfigureStates() { }
    
    private void EnterOn() { }
    private void ExitOn() { }
}

public enum PhoneState 
{ 
    Off,
    On,
    Idle,
    Calling,
    InCall
}

public enum PhoneTrigger 
{ 
    PowerOn,
    PowerOff,
    Dial,
    Connect,
    Hangup,
    Cancel
}