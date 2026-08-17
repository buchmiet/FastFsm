using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

[StateMachine(typeof(CallbackState), typeof(CallbackTrigger))]
public partial class CallbackOrderMachine
{
    public List<string> ExecutionLog { get; } = [];

    private void Configure() => FSM
        .State(CallbackState.A)
        .OnExit((OnExitA))
        .On(CallbackTrigger.Next).Action((ActionAtoB)).GoTo(CallbackState.B)
        .State(CallbackState.B)
        .OnEntry((OnEntryB)).OnExit((OnExitB))
        .On(CallbackTrigger.Next).Action((ActionBtoC)).GoTo(CallbackState.C)
        .State(CallbackState.C)
        .OnEntry((OnEntryC));

    private void OnExitA() => ExecutionLog.Add("Exit-A");
    private void OnEntryB() => ExecutionLog.Add("Entry-B");
    private void OnExitB() => ExecutionLog.Add("Exit-B");
    private void OnEntryC() => ExecutionLog.Add("Entry-C");
    private void ActionAtoB() => ExecutionLog.Add("Action-A-to-B");
    private void ActionBtoC() => ExecutionLog.Add("Action-B-to-C");
}