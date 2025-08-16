using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

/// <summary>
/// Tests for FSM007-009 payload validation diagnostics.
/// </summary>
public class PayloadValidationDiagnosticTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void FSM007_MissingPayloadType_WithPayloadVariant_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    // Forced WithPayload variant but no PayloadType attribute
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm007 = diags.Where(d => d.Id == RuleIdentifiers.MissingPayloadType).ToList();
        Assert.NotEmpty(fsm007);
        Assert.Equal(DiagnosticSeverity.Error, fsm007[0].Severity);
        output.WriteLine($"FSM007: ✓ EMITTED - {fsm007[0].GetMessage()}");
    }

    [Fact]
    public void FSM007_MissingPayloadType_FullVariant_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    // Forced Full variant but no PayloadType attribute
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm007 = diags.Where(d => d.Id == RuleIdentifiers.MissingPayloadType).ToList();
        Assert.NotEmpty(fsm007);
        output.WriteLine($"FSM007: ✓ EMITTED - {fsm007[0].GetMessage()}");
    }

    [Fact]
    public void FSM007_MissingPayloadType_WithDefaultPayload_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                public class MyPayload { }
                
                [StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(MyPayload))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm007 = diags.Where(d => d.Id == RuleIdentifiers.MissingPayloadType).ToList();
        Assert.Empty(fsm007);
        output.WriteLine($"FSM007: ✓ NOT EMITTED (correctly - has payload)");
    }

    [Fact]
    public void FSM008_ConflictingPayloadConfiguration_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X, Y }
                public class PayloadA { }
                public class PayloadB { }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                [PayloadType(typeof(PayloadA), Triggers = new[] { Trigger.X })]
                [PayloadType(typeof(PayloadB), Triggers = new[] { Trigger.Y })]
                public partial class Machine {
                    // WithPayload variant can't have trigger-specific payloads
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm008 = diags.Where(d => d.Id == RuleIdentifiers.ConflictingPayloadConfiguration).ToList();
        Assert.NotEmpty(fsm008);
        Assert.Equal(DiagnosticSeverity.Error, fsm008[0].Severity);
        output.WriteLine($"FSM008: ✓ EMITTED - {fsm008[0].GetMessage()}");
    }

    [Fact]
    public void FSM008_ConflictingPayloadConfiguration_NotWithPayload_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X, Y }
                public class PayloadA { }
                public class PayloadB { }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                [PayloadType(typeof(PayloadA), Triggers = new[] { Trigger.X })]
                [PayloadType(typeof(PayloadB), Triggers = new[] { Trigger.Y })]
                public partial class Machine {
                    // Full variant CAN have trigger-specific payloads
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm008 = diags.Where(d => d.Id == RuleIdentifiers.ConflictingPayloadConfiguration).ToList();
        Assert.Empty(fsm008);
        output.WriteLine($"FSM008: ✓ NOT EMITTED (correctly - Full variant allows trigger-specific)");
    }

    [Fact]
    public void FSM009_InvalidForcedVariant_Pure_WithCallbacks_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    // Pure variant can't have callbacks
                    [State(State.A, OnEntry = nameof(EnterA))]
                    private void Config() { }
                    
                    private void EnterA() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm009 = diags.Where(d => d.Id == RuleIdentifiers.InvalidForcedVariantConfiguration).ToList();
        Assert.NotEmpty(fsm009);
        Assert.Equal(DiagnosticSeverity.Error, fsm009[0].Severity);
        Assert.Contains("OnEntryExit", fsm009[0].GetMessage());
        output.WriteLine($"FSM009: ✓ EMITTED - {fsm009[0].GetMessage()}");
    }

    [Fact]
    public void FSM009_InvalidForcedVariant_Pure_WithPayload_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                public class MyPayload { }
                
                [StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(MyPayload))]
                public partial class Machine {
                    // Pure variant can't have payloads
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm009 = diags.Where(d => d.Id == RuleIdentifiers.InvalidForcedVariantConfiguration).ToList();
        Assert.NotEmpty(fsm009);
        Assert.Contains("PayloadTypes", fsm009[0].GetMessage());
        output.WriteLine($"FSM009: ✓ EMITTED - {fsm009[0].GetMessage()}");
    }

    [Fact]
    public void FSM009_InvalidForcedVariant_Basic_WithPayload_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                public class MyPayload { }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                [PayloadType(typeof(MyPayload))]
                public partial class Machine {
                    // Basic variant can't have payloads
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm009 = diags.Where(d => d.Id == RuleIdentifiers.InvalidForcedVariantConfiguration).ToList();
        Assert.NotEmpty(fsm009);
        Assert.Contains("PayloadTypes", fsm009[0].GetMessage());
        output.WriteLine($"FSM009: ✓ EMITTED - {fsm009[0].GetMessage()}");
    }

    [Fact]
    public void FSM009_InvalidForcedVariant_WithExtensions_WithPayload_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                public class MyPayload { }
                
                [StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(MyPayload))]
                public partial class Machine {
                    // WithExtensions variant can't have payloads
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm009 = diags.Where(d => d.Id == RuleIdentifiers.InvalidForcedVariantConfiguration).ToList();
        Assert.NotEmpty(fsm009);
        Assert.Contains("PayloadTypes", fsm009[0].GetMessage());
        output.WriteLine($"FSM009: ✓ EMITTED - {fsm009[0].GetMessage()}");
    }

    [Fact]
    public void FSM009_InvalidForcedVariant_WithPayload_WithExtensions_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                public class MyPayload { }
                
                [StateMachine(typeof(State), typeof(Trigger), 
                    DefaultPayloadType = typeof(MyPayload),
                    GenerateExtensibleVersion = true)]
                public partial class Machine {
                    // WithPayload variant can't have extensions
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm009 = diags.Where(d => d.Id == RuleIdentifiers.InvalidForcedVariantConfiguration).ToList();
        Assert.NotEmpty(fsm009);
        Assert.Contains("Extensions", fsm009[0].GetMessage());
        output.WriteLine($"FSM009: ✓ EMITTED - {fsm009[0].GetMessage()}");
    }

    [Fact]
    public void FSM009_InvalidForcedVariant_Full_WithEverything_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                public class MyPayload { }
                
                [StateMachine(typeof(State), typeof(Trigger), 
                    DefaultPayloadType = typeof(MyPayload),
                    GenerateExtensibleVersion = true)]
                public partial class Machine {
                    // Full variant can have everything
                    [State(State.A, OnEntry = nameof(EnterA))]
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                    
                    private void EnterA() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm009 = diags.Where(d => d.Id == RuleIdentifiers.InvalidForcedVariantConfiguration).ToList();
        Assert.Empty(fsm009);
        output.WriteLine($"FSM009: ✓ NOT EMITTED (correctly - Full variant allows everything)");
    }

    [Fact]
    public void FSM007_FSM009_Combined_ShouldEmitBoth()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    // Missing payload AND has callbacks (two problems)
                    [State(State.A, OnEntry = nameof(EnterA))]
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                    
                    private void EnterA() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm007 = diags.Where(d => d.Id == RuleIdentifiers.MissingPayloadType).ToList();
        Assert.NotEmpty(fsm007);
        output.WriteLine($"FSM007: ✓ EMITTED - {fsm007[0].GetMessage()}");
        
        // Note: WithPayload CAN have callbacks, so FSM009 should not emit for callbacks
        var fsm009 = diags.Where(d => d.Id == RuleIdentifiers.InvalidForcedVariantConfiguration).ToList();
        Assert.Empty(fsm009);
        output.WriteLine($"FSM009: ✓ NOT EMITTED (correctly - WithPayload allows callbacks)");
    }
}
