using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Fluent;

/// <summary>
/// Tests for FSM3070: Ambiguous method group reference
/// </summary>
public class FSM3070_AmbiguousMethodGroupTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3070_For_Ambiguous_Guard_Sync_Overloads()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine
    {
        public enum State { A, B }
        public enum Trigger { Go }

        private bool CanGo() => true;
        private bool CanGo(int unused) => false; // Different signature, but ambiguous for method group

        private static void Configure() => FSM
            .State(State.A)
                .On(Trigger.Go)
                    .Guard(CanGo)  // Ambiguous!
                    .GoTo(State.B);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3070 for ambiguous method group 'CanGo'.");
    }

    [Fact]
    public void Emits_FSM3070_For_Ambiguous_Guard_Payload_Overloads()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    public class PayloadA { public int Value { get; set; } }

    [StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(PayloadA))]
    public partial class TestMachine
    {
        public enum State { Idle, Working }
        public enum Trigger { Start }

        private bool Validate() => true;
        private bool Validate(in PayloadA payload) => payload.Value > 0;

        private static void Configure() => FSM
            .State(State.Idle)
                .On(Trigger.Start)
                    .Guard(Validate)  // Ambiguous!
                    .GoTo(State.Working);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3070 for ambiguous method group 'Validate'.");
    }

    [Fact]
    public void Emits_FSM3070_For_Ambiguous_Guard_Async_Overloads()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine
    {
        public enum State { Ready, Busy }
        public enum Trigger { Process }

        private bool CheckReady() => true;
        private ValueTask<bool> CheckReady(CancellationToken ct) => ValueTask.FromResult(true);

        private static void Configure() => FSM
            .State(State.Ready)
                .On(Trigger.Process)
                    .Guard(CheckReady)  // Ambiguous between sync and async!
                    .GoTo(State.Busy);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3070 for ambiguous method group 'CheckReady'.");
    }

    [Fact]
    public void NoError_For_NonAmbiguous_Guard_Single_Method()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine
    {
        public enum State { A, B }
        public enum Trigger { Go }

        private bool CanGo() => true; // Only one method with this name

        private static void Configure() => FSM
            .State(State.A)
                .On(Trigger.Go)
                    .Guard(CanGo)  // NOT ambiguous - should work fine
                    .GoTo(State.B);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.Empty(hits); // Should NOT emit FSM3070
    }

    [Fact]
    public void NoError_For_Guard_Using_Nameof_Even_With_Overloads()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine
    {
        public enum State { A, B }
        public enum Trigger { Go }

        private bool CanGo() => true;
        private bool CanGo(int unused) => false;

        private static void Configure() => FSM
            .State(State.A)
                .On(Trigger.Go)
                    .Guard(nameof(CanGo))  // Using nameof - should NOT trigger FSM3070
                    .GoTo(State.B);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.Empty(hits); // nameof should not trigger FSM3070
    }

    [Fact]
    public void Emits_FSM3070_For_Multiple_Candidates()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    public class Data { public int Value { get; set; } }

    [StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(Data))]
    public partial class TestMachine
    {
        public enum State { X, Y }
        public enum Trigger { Move }

        // Three overloads with same name
        private bool Check() => true;
        private bool Check(in Data data) => data.Value > 0;
        private ValueTask<bool> Check(in Data data, CancellationToken ct)
            => ValueTask.FromResult(data.Value > 10);

        private static void Configure() => FSM
            .State(State.X)
                .On(Trigger.Move)
                    .Guard(Check)  // Ambiguous with 3 candidates!
                    .GoTo(State.Y);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3070 for ambiguous method group with 3 candidates.");
    }

    [Fact]
    public void Emits_FSM3070_For_Member_Access_Expression()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine
    {
        public enum State { Init, Done }
        public enum Trigger { Execute }

        private bool IsReady() => true;
        private bool IsReady(string context) => context != null;

        private static void Configure() => FSM
            .State(State.Init)
                .On(Trigger.Execute)
                    .Guard(this.IsReady)  // Ambiguous even with member access!
                    .GoTo(State.Done);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3070 for ambiguous member access method group.");
    }

    [Fact]
    public void Emits_FSM3070_For_Static_Vs_Instance_Methods()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine
    {
        public enum State { A, B }
        public enum Trigger { T }

        private bool Verify() => true;
        private static bool Verify(int x) => x > 0;

        private static void Configure() => FSM
            .State(State.A)
                .On(Trigger.T)
                    .Guard(Verify)  // Ambiguous between instance and static
                    .GoTo(State.B);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3070 for ambiguous static vs instance method group.");
    }

    [Fact]
    public void Emits_FSM3070_For_Inheritance_Scenario()
    {
        const string src = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    public abstract class BaseValidator
    {
        protected bool Validate() => true;
    }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine : BaseValidator
    {
        public enum State { A, B }
        public enum Trigger { T }

        private new bool Validate(int x) => x > 0; // Hides base method

        private static void Configure() => FSM
            .State(State.A)
                .On(Trigger.T)
                    .Guard(Validate)  // Ambiguous!
                    .GoTo(State.B);
    }
}";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AmbiguousMethodGroup).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3070 for ambiguous method group with inheritance.");
    }
}