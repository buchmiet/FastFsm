using Abstractions.Attributes;

[StateMachine(typeof(MyState), typeof(MyTrigger))]
public partial class SimpleMachine 
{
    [Transition(MyState.A, MyTrigger.Next, MyState.B)]
    private void Configure() { }
}

public enum MyState { A, B }
public enum MyTrigger { Next }
