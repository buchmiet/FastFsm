using Abstractions.Attributes;
﻿namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(MultipleCallbacksState), typeof(MultipleCallbacksTrigger))]
public partial class MultipleCallbacksMachine
{
    public List<string> Log { get; } = [];

    // Multiple state attributes for same state
    [State(MultipleCallbacksState.A, OnEntry = nameof(OnEntry1))]
    [State(MultipleCallbacksState.A, OnEntry = nameof(OnEntry2))] // This might override
    private void ConfigureStates() { }

    [Transition(MultipleCallbacksState.A, MultipleCallbacksTrigger.Go, MultipleCallbacksState.B)]
    private void Configure() { }

    private void OnEntry1() => Log.Add("Entry1");
    private void OnEntry2() => Log.Add("Entry2");
}
