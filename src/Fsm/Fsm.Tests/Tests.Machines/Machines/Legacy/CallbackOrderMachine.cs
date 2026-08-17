using Abstractions.Fluent;

namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(CallbackState), typeof(CallbackTrigger))]
public partial class CallbackOrderMachine
{
    public List<string> ExecutionLog { get; } = [];

    private void Configure() => FSM
        .State(CallbackState.A)
        .OnExit(nameof(OnExitA))
        .On(CallbackTrigger.Next).Action(nameof(ActionAtoB)).GoTo(CallbackState.B)
        .State(CallbackState.B)
        .OnEntry(nameof(OnEntryB)).OnExit(nameof(OnExitB))
        .On(CallbackTrigger.Next).Action(nameof(ActionBtoC)).GoTo(CallbackState.C)
        .State(CallbackState.C)
        .OnEntry(nameof(OnEntryC));

    private void OnExitA() => ExecutionLog.Add("Exit-A");
    private void OnEntryB() => ExecutionLog.Add("Entry-B");
    private void OnExitB() => ExecutionLog.Add("Exit-B");
    private void OnEntryC() => ExecutionLog.Add("Entry-C");
    private void ActionAtoB() => ExecutionLog.Add("Action-A-to-B");
    private void ActionBtoC() => ExecutionLog.Add("Action-B-to-C");
}