using Abstractions.Attributes;
﻿namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class BasicBenchmarkMachine
{
    private int _counter;

    [State(BenchmarkState.A, OnEntry = (IncrementCounter))]
    [State(BenchmarkState.B, OnEntry = (IncrementCounter))]
    [State(BenchmarkState.C, OnEntry = (IncrementCounter))]
    [State(BenchmarkState.D, OnEntry = (IncrementCounter))]
    private void ConfigureStates() { }

    [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B)]
    [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.C)]
    [Transition(BenchmarkState.C, BenchmarkTrigger.Next, BenchmarkState.D)]
    [Transition(BenchmarkState.D, BenchmarkTrigger.Next, BenchmarkState.A)]
    private void Configure() { }

    private void IncrementCounter() => _counter++;
}
