using Abstractions.Attributes;
using Abstractions.Fluent;
using System.Threading;
using System.Threading.Tasks;

namespace ParserComparison.Tests;

#region Test 1: No payload, sync guard with method group

[StateMachine(typeof(SimpleGuardStates), typeof(SimpleGuardTriggers))]
public partial class SimpleGuardMethodGroupMachine
{
    public enum SimpleGuardStates { A, B }
    public enum SimpleGuardTriggers { Go }

    private bool CanGo() => true;

    private static void Configure() => FSM
        .State(SimpleGuardStates.A)
            .On(SimpleGuardTriggers.Go)
                .Guard(CanGo)  // Method group instead of nameof(CanGo)
                .GoTo(SimpleGuardStates.B);
}

#endregion

#region Test 2: No payload, async guard with CancellationToken

[StateMachine(typeof(AsyncGuardStates), typeof(AsyncGuardTriggers))]
public partial class AsyncGuardMethodGroupMachine
{
    public enum AsyncGuardStates { A, B }
    public enum AsyncGuardTriggers { Go }

    private ValueTask<bool> CanReturnAsync(CancellationToken ct) => ValueTask.FromResult(true);

    private static void Configure() => FSM
        .State(AsyncGuardStates.B)
            .On(AsyncGuardTriggers.Go).GoTo(AsyncGuardStates.A)
                .Guard(CanReturnAsync);  // Method group for async guard
}

#endregion

#region Test 3: Single payload via DefaultPayloadType (sync)

public sealed class MyPayload
{
    public int Value { get; init; }
}

[StateMachine(typeof(PayloadGuardStates), typeof(PayloadGuardTriggers), DefaultPayloadType = typeof(MyPayload))]
public partial class PayloadGuardMethodGroupMachine
{
    public enum PayloadGuardStates { Idle, Run }
    public enum PayloadGuardTriggers { Start }

    private bool CanWithPayload(in MyPayload p) => p.Value > 0;

    private static void Configure() => FSM
        .State(PayloadGuardStates.Idle)
            .On(PayloadGuardTriggers.Start).GoTo(PayloadGuardStates.Run)
                .Guard(CanWithPayload);  // Method group with payload
}

#endregion

#region Test 4: Single payload via .Payload<T>() override (async)

[StateMachine(typeof(OverridePayloadStates), typeof(OverridePayloadTriggers))]
public partial class OverridePayloadGuardMachine
{
    public enum OverridePayloadStates { Run, Idle }
    public enum OverridePayloadTriggers { Tick }

    private ValueTask<bool> CanWithPayloadAsync(in MyPayload p, CancellationToken ct)
        => ValueTask.FromResult(p.Value > 10);

    private static void Configure() => FSM
        .State(OverridePayloadStates.Run)
            .On(OverridePayloadTriggers.Tick)
                .Payload<MyPayload>()
                .Internal()
                .Guard(CanWithPayloadAsync);  // Method group with explicit payload type
}

#endregion

#region Test 5: Multi payload per trigger via [PayloadType]

public sealed class ApplyPayload
{
    public string Id { get; init; } = "";
}

[PayloadType(MultiPayloadTriggers.Apply, typeof(ApplyPayload))]
[StateMachine(typeof(MultiPayloadStates), typeof(MultiPayloadTriggers))]
public partial class MultiPayloadGuardMachine
{
    public enum MultiPayloadStates { S1, S2 }
    public enum MultiPayloadTriggers { Apply, Reset }

    private bool CanApply(in ApplyPayload p) => !string.IsNullOrEmpty(p.Id);

    private static void Configure() => FSM
        .State(MultiPayloadStates.S1)
            .On(MultiPayloadTriggers.Apply).GoTo(MultiPayloadStates.S2)
                .Guard(CanApply);  // Method group with trigger-specific payload
}

#endregion

#region Test 6: Back-compat - nameof still works

[StateMachine(typeof(BackCompatStates), typeof(BackCompatTriggers))]
public partial class BackCompatGuardMachine
{
    public enum BackCompatStates { A, B }
    public enum BackCompatTriggers { Go }

    private bool CanGo() => true;

    private static void Configure() => FSM
        .State(BackCompatStates.A)
            .On(BackCompatTriggers.Go).GoTo(BackCompatStates.B)
                .Guard(nameof(CanGo));  // Old style with nameof - should still work
}

#endregion

#region Test 7: Ambiguous method group - should emit FSM3070

[StateMachine(typeof(AmbiguousStates), typeof(AmbiguousTriggers))]
public partial class AmbiguousGuardMachine
{
    public enum AmbiguousStates { A, B }
    public enum AmbiguousTriggers { Go }

    // Two overloads with same name - ambiguous for method group
    private bool Ambiguous() => true;
    private bool Ambiguous(in MyPayload p) => p.Value > 0;

    private static void Configure() => FSM
        .State(AmbiguousStates.A)
            .On(AmbiguousTriggers.Go).GoTo(AmbiguousStates.B)
                .Guard(Ambiguous);  // Should emit FSM3070 - ambiguous method group
}

#endregion

#region Test 8: Mixed - method groups and nameof in same machine

[StateMachine(typeof(MixedStates), typeof(MixedTriggers), DefaultPayloadType = typeof(MyPayload))]
public partial class MixedGuardMachine
{
    public enum MixedStates { Start, Middle, End }
    public enum MixedTriggers { First, Second }

    private bool CanGoFirst(in MyPayload p) => p.Value > 5;
    private ValueTask<bool> CanGoSecondAsync(in MyPayload p, CancellationToken ct)
        => ValueTask.FromResult(p.Value < 100);

    private static void Configure() => FSM
        .State(MixedStates.Start)
            .On(MixedTriggers.First).GoTo(MixedStates.Middle)
                .Guard(CanGoFirst)  // Method group
        .State(MixedStates.Middle)
            .On(MixedTriggers.Second).GoTo(MixedStates.End)
                .Guard(nameof(CanGoSecondAsync));  // nameof for comparison
}

#endregion

#region Test 9: Member access method group (this.Method)

[StateMachine(typeof(MemberAccessStates), typeof(MemberAccessTriggers))]
public partial class MemberAccessGuardMachine
{
    public enum MemberAccessStates { Active, Inactive }
    public enum MemberAccessTriggers { Toggle }

    private bool IsReady() => true;

    private static void Configure() => FSM
        .State(MemberAccessStates.Active)
            .On(MemberAccessTriggers.Toggle).GoTo(MemberAccessStates.Inactive)
                .Guard(this.IsReady);  // Member access expression as method group
}

#endregion

#region Test 10: Generic payload with method group

public record struct GenericPayload<T>(T Value);

[StateMachine(typeof(GenericStates), typeof(GenericTriggers), DefaultPayloadType = typeof(GenericPayload<int>))]
public partial class GenericPayloadGuardMachine
{
    public enum GenericStates { Init, Process, Done }
    public enum GenericTriggers { Start, Complete }

    private bool CanStart(in GenericPayload<int> p) => p.Value > 0;
    private bool CanComplete(in GenericPayload<int> p) => p.Value == 100;

    private static void Configure() => FSM
        .State(GenericStates.Init)
            .On(GenericTriggers.Start).GoTo(GenericStates.Process)
                .Guard(CanStart)  // Method group with generic payload
        .State(GenericStates.Process)
            .On(GenericTriggers.Complete).GoTo(GenericStates.Done)
                .Guard(CanComplete);
}

#endregion

#region Test 11: Complex scenario with guards and actions

[StateMachine(typeof(ComplexStates), typeof(ComplexTriggers), DefaultPayloadType = typeof(MyPayload))]
public partial class ComplexGuardActionMachine
{
    public enum ComplexStates { Ready, Working, Finished, Failed }
    public enum ComplexTriggers { Start, Process, Complete, Fail }

    private int _workDone = 0;

    private bool CanStart(in MyPayload p) => p.Value > 0 && p.Value <= 100;
    private void OnStart(in MyPayload p) => _workDone = p.Value;

    private bool CanProcess() => _workDone < 100;
    private void DoWork() => _workDone += 10;

    private bool CanComplete() => _workDone >= 100;
    private async Task CompleteAsync(CancellationToken ct)
    {
        await Task.Delay(10, ct);
        _workDone = 0;
    }

    private static void Configure() => FSM
        .State(ComplexStates.Ready)
            .On(ComplexTriggers.Start).GoTo(ComplexStates.Working)
                .Guard(CanStart)
                .Action(nameof(OnStart))  // Mix: action still uses nameof for now
        .State(ComplexStates.Working)
            .On(ComplexTriggers.Process).Internal()
                .Guard(CanProcess)  // Method group
                .Action(nameof(DoWork))
            .On(ComplexTriggers.Complete).GoTo(ComplexStates.Finished)
                .Guard(CanComplete)  // Method group
                .Action(nameof(CompleteAsync))
            .On(ComplexTriggers.Fail).GoTo(ComplexStates.Failed);
}

#endregion