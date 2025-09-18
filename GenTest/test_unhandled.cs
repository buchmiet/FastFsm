using Abstractions.Attributes;

public enum S { A, B }
public enum T { X, Y }

[StateMachine(typeof(S), typeof(T), GenerateExtensibleVersion = true)]
public partial class M
{
    [Transition(S.A, T.X, S.B)]
    private void Cfg() { }
}

// Enable extensions via Fluent API for lenient mode
Abstractions.Fluent.FSM.Extensible<S>();
