using System.Collections.Generic;
using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(MultiState), typeof(MultiTrigger))]
public partial class MultipleCallbacksMachineLegacy
{
    public List<string> Log { get; } = [];

    // Multiple state attributes for same state
    [State(MultiState.A, OnEntry = nameof(OnEntry1))]
    [State(MultiState.A, OnEntry = nameof(OnEntry2))] // This might override
    private void ConfigureStates() { }

    [Transition(MultiState.A, MultiTrigger.Go, MultiState.B)]
    private void Configure() { }

    private void OnEntry1() => Log.Add("Entry1");
    private void OnEntry2() => Log.Add("Entry2");
}