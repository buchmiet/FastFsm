using Abstractions.Attributes;
﻿using Abstractions.Fluent;

namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class NoGuardBenchmarkMachine
{
    private void Configure() => FSM
        .State(BenchmarkState.A)
            .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
            .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.A);
}

