namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(InternalMachine_S), typeof(InternalMachine_T), EnableHierarchy = true)]
public partial class InternalMachine
{
    public List<string> Log { get; } = new();
    public bool UseChildInternal { get; set; }

    [State(InternalMachine_S.Parent, OnEntry = nameof(OnParentEntry))] private void Parent() { }
    [State(InternalMachine_S.Child, Parent = InternalMachine_S.Parent, IsInitial = true,
        OnEntry = nameof(OnChildEntry), OnExit = nameof(OnChildExit))]
    private void Child() { }

    // Parent internal (always present)
    [InternalTransition(InternalMachine_S.Parent, InternalMachine_T.Refresh, Action = nameof(ParentInternalAction))]
    private void ParentInternals() { }

    // Child internal (conditionally compiled in generator regardless of UseChildInternal flag),
    // but we decide at runtime which action to log.
    [InternalTransition(InternalMachine_S.Child, InternalMachine_T.Refresh, Guard = nameof(UseChildInternalGuard), Action = nameof(ChildInternalAction))]
    private void ChildInternals() { }
    private void ParentInternalAction() => Log.Add("ParentInternal");
    private void ChildInternalAction() => Log.Add("ChildInternal");
    private bool UseChildInternalGuard() => UseChildInternal;

    private void OnParentEntry() { }
    private void OnChildEntry() => Log.Add("OnEntryChild");
    private void OnChildExit() => Log.Add("OnExitChild");
}
