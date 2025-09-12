namespace Dsl;

// Stubbed DSL for Fluent API testing
public static class FSM
{
    public static FsmDsl State(object _) => new FsmDsl();
    public static FsmDsl At(object _) => new FsmDsl();
}

public sealed class FsmDsl
{
    // HSM methods
    public FsmDsl ChildOf(object _) => this;
    public FsmDsl Initial(object _) => this;
    public FsmDsl HistoryShallow() => this;
    public FsmDsl HistoryDeep() => this;
        
    // States and transitions
    public FsmDsl State(object _) => this;
    public FsmDsl On(object _) => this;
    public FsmDsl OnInternal(object _) => this;
    public FsmDsl GoTo(object _) => this;
    public FsmDsl Internal() => this;
        
    // Entry/Exit callbacks
    public FsmDsl OnEntry(string _) => this;
    public FsmDsl OnEntryAsync(string _) => this;
    public FsmDsl OnExit(string _) => this;
    public FsmDsl OnExitAsync(string _) => this;
        
    // Guards and Actions
    public FsmDsl Guard(string _) => this;
    public FsmDsl GuardAsync(string _) => this;
    public FsmDsl Action(string _) => this;
    public FsmDsl ActionAsync(string _) => this;
    public FsmDsl Payload(System.Type _) => this;
    public FsmDsl Priority(int _) => this;
        
    // Overloads for optional parameters
    public FsmDsl Guard() => this;
    public FsmDsl Action() => this;
    public FsmDsl Payload() => this;
}