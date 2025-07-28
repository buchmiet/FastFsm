using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public class DiagnosticTests(ITestOutputHelper output):GeneratorBaseClass(output)
{

    [Fact]
    public void FullVariant_WithPayloadAction_DoesNotGenerateRedundantCode()
    {
        // Arrange: Prosta maszyna z wariantem Full i akcją z payloadem.
        const string source = """
                          using Abstractions.Attributes;
                          
                          namespace TestNamespace 
                          {
                              public enum State { A, B }
                              public enum Trigger { Go }
                              public class MyPayload { public int Value { get; set; } }

                              [StateMachine(typeof(State), typeof(Trigger))]
                              [PayloadType(typeof(MyPayload))]
                              [GenerationMode(GenerationMode.Full, Force = true)]
                              public partial class RedundantCodeTestMachine
                              {
                                  [Transition(State.A, Trigger.Go, State.B, Action = nameof(MyAction))]
                                  private void Cfg() { }
                                  
                                  private void MyAction(MyPayload payload) { }
                              }
                          }
                          """;

        // Act
        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        // Assert: Kompilacja musi przejść bez błędów.
        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
        Assert.NotNull(asm);

        // Zdobądź wygenerowany kod maszyny.
        var generatedCode = generatedSources["RedundantCodeTestMachine.Generated.cs"];
        Assert.NotNull(generatedCode);

        output.WriteLine("// ----- GENERATED CODE FOR RedundantCodeTestMachine -----");
        output.WriteLine(generatedCode);
        output.WriteLine("// ----------------------------------------------------");

        // Policz, ile razy wywoływana jest akcja "MyAction".
        // Używamy wyrażenia regularnego, aby uniknąć fałszywych trafień w komentarzach itp.
        var actionCalls = System.Text.RegularExpressions.Regex.Matches(generatedCode, @"MyAction\(.*?\);");

        // Asercja: Powinno być DOKŁADNIE jedno wywołanie. Jeśli jest więcej, to jest błąd.
        Assert.Equal(1, actionCalls.Count);
    }

    [Fact]
    public void FullMultiPayloadMachine_GeneratesCorrectCode_AndPrintsIt()
    {
        // ---------- 1. Źródła użytkownika (skopiowane z definicji maszyny testowej) ----------
        const string machineSource = """
                                 using System;
                                 using Abstractions.Attributes;

                                 namespace StateMachine.Tests.Machines
                                 {
                                     // Definicje payloadów i enumów, żeby kod był kompletny
                                     public enum OrderState { New, Processing, Paid, Shipped, Delivered, Cancelled }
                                     public enum OrderTrigger { Process, Pay, Ship, Deliver, Cancel, Refund }

                                     public class OrderPayload
                                     {
                                         public int OrderId { get; set; }
                                         public decimal Amount { get; set; }
                                         public string? TrackingNumber { get; set; }
                                     }
                                     public class PaymentPayload : OrderPayload
                                     {
                                         public string PaymentMethod { get; set; } = "";
                                         public DateTime PaymentDate { get; set; }
                                     }
                                     public class ShippingPayload : OrderPayload
                                     {
                                         public string Carrier { get; set; } = "";
                                         public DateTime EstimatedDelivery { get; set; }
                                     }
                                     
                                     // Definicja maszyny, która sprawia problemy
                                     [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
                                     [PayloadType(OrderTrigger.Process, typeof(OrderPayload))]
                                     [PayloadType(OrderTrigger.Pay, typeof(PaymentPayload))]
                                     [PayloadType(OrderTrigger.Ship, typeof(ShippingPayload))]
                                     [GenerationMode(GenerationMode.Full, Force = true)]
                                     public partial class FullMultiPayloadMachine
                                     {
                                       [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing,
                                             Action = nameof(HandleOrder))]
                                       [Transition(OrderState.Processing, OrderTrigger.Pay, OrderState.Paid,
                                             Action = nameof(HandlePayment))]
                                       [Transition(OrderState.Paid, OrderTrigger.Ship, OrderState.Shipped,
                                             Action = nameof(HandleShipping))]
                                       private void Configure() { }

                                       private void HandleOrder(OrderPayload order) { }
                                       private void HandlePayment(PaymentPayload payment) { }
                                       private void HandleShipping(ShippingPayload shipping) { }
                                     }
                                 }
                                 """;

        // ---------- 2. Kompilacja + uruchomienie generatora ----------
        var (asm, diags, generatedSources) =
            CompileAndRunGenerator([machineSource], new StateMachineGenerator());

        // ---------- 3. Wypisanie wygenerowanych źródeł ----------
        OutputGeneratedCode(generatedSources, "FullMultiPayloadMachine");

        // ---------- 4. Asercje ----------
        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors); // Jeśli to się nie powiedzie, problem jest już na etapie parsowania/kompilacji
        Assert.NotNull(asm);

        // Sprawdźmy kluczowe elementy wygenerowanego kodu
        var generatedFile = generatedSources["FullMultiPayloadMachine.Generated.cs"];
        Assert.NotNull(generatedFile);

        // Czy wygenerowano mapę payloadów?
        Assert.Contains("private static readonly Dictionary<OrderTrigger, Type> _payloadMap", generatedFile);

        // Czy poprawnie rzutuje payload dla akcji HandleOrder?
        Assert.Contains("if (payload is OrderPayload typedActionPayload)", generatedFile);
        Assert.Contains("HandleOrder(typedActionPayload);", generatedFile);

        // Czy poprawnie rzutuje payload dla akcji HandlePayment?
        Assert.Contains("if (payload is PaymentPayload typedActionPayload)", generatedFile);
        Assert.Contains("HandlePayment(typedActionPayload);", generatedFile);

        // Czy poprawnie rzutuje payload dla akcji HandleShipping?
        Assert.Contains("if (payload is ShippingPayload typedActionPayload)", generatedFile);
        Assert.Contains("HandleShipping(typedActionPayload);", generatedFile);
    }

    // Pomocnicza metoda do wypisywania kodu (już ją masz, ale dla pewności)
    private void OutputGeneratedCode(Dictionary<string, string> generatedSources, string filterKeyword)
    {
        foreach (var source in generatedSources.Where(kvp => kvp.Key.Contains(filterKeyword)))
        {
            output.WriteLine($"// ----- GENERATED FILE: {source.Key} -----");
            output.WriteLine(source.Value);
            output.WriteLine($"// ----- END OF FILE: {source.Key} -----\n");
        }
    }

    [Theory]
    [InlineData(RuleIdentifiers.DuplicateTransition, @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { Go }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.Go, State.B)]
                    [Transition(State.A, Trigger.Go, State.B)] // Duplicate!
                    private void Config() { }
                }
            }")]
    [InlineData(RuleIdentifiers.UnreachableState, @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { Start, Connected, Orphaned }
                public enum Trigger { Connect }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.Start, Trigger.Connect, State.Connected)]
                    // Note: State.Orphaned is unreachable
                    private void Config() { }
                }
            }")]
    [InlineData(RuleIdentifiers.InvalidMethodSignature, @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { Go }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.Go, State.B, Guard = nameof(BadGuard))]
                    private void Config() { }
                    
                    private void BadGuard() { } // Should return bool
                }
            }")]
    [InlineData(RuleIdentifiers.MissingStateMachineAttribute, @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { Go }
                
                // Missing [StateMachine] attribute
                public partial class Machine {
                    [Transition(State.A, Trigger.Go, State.B)]
                    private void Config() { }
                }
            }")]
    [InlineData(RuleIdentifiers.InvalidTypesInAttribute, @"
            using Abstractions.Attributes;
            namespace Test {
                public class NotAnEnum { }
                public enum Trigger { Go }
                
                [StateMachine(typeof(NotAnEnum), typeof(Trigger))] // Not an enum!
                public partial class Machine {
                    private void Config() { }
                }
            }")]
    [InlineData(RuleIdentifiers.InvalidEnumValueInTransition, @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { Go }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition((State)999, Trigger.Go, State.B)] // Invalid enum value
                    private void Config() { }
                }
            }")]
    public void Generator_ReportsExpectedDiagnostic(string expectedDiagnosticId, string sourceCode)
    {
        // Arrange
           
        var (asm1, diags1, generatedSources1) =
            CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        // Act
        var diagnostic = diags1.FirstOrDefault(d => d.Id == expectedDiagnosticId);

        // Assert
        Assert.NotNull(diagnostic);
        output.WriteLine($"Found diagnostic {expectedDiagnosticId}: {diagnostic?.GetMessage()}");

        // Additional assertions based on diagnostic type
        switch (expectedDiagnosticId)
        {
            case RuleIdentifiers.DuplicateTransition:
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic?.Severity);
                Assert.Contains("Duplicate transition", diagnostic?.GetMessage());
                break;
            case RuleIdentifiers.UnreachableState:
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic?.Severity);
                Assert.Contains("unreachable", diagnostic?.GetMessage());
                break;
            case RuleIdentifiers.InvalidMethodSignature:
                Assert.Equal(DiagnosticSeverity.Error, diagnostic?.Severity);
                Assert.Contains("invalid signature", diagnostic?.GetMessage());
                break;
            case RuleIdentifiers.MissingStateMachineAttribute:
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic?.Severity);
                Assert.Contains("missing", diagnostic?.GetMessage());
                break;
            case RuleIdentifiers.InvalidTypesInAttribute:
                Assert.Equal(DiagnosticSeverity.Error, diagnostic?.Severity);
                Assert.Contains("must be an enum", diagnostic?.GetMessage());
                break;
            case RuleIdentifiers.InvalidEnumValueInTransition:
                Assert.Equal(DiagnosticSeverity.Error, diagnostic?.Severity);
                Assert.Contains("Invalid enum value", diagnostic?.GetMessage());
                break;
        }
    }

    [Fact]
    public void MultipleErrors_AllReported()
    {
        // Arrange - Source with multiple issues
        const string sourceCode = @"
                using Abstractions.Attributes;
                namespace Test {
                    public enum State { A, B, C }
                    public enum Trigger { Go, Back }
                    
                    [StateMachine(typeof(State), typeof(Trigger))]
                    public partial class Machine {
                        [Transition(State.A, Trigger.Go, State.B)]
                        [Transition(State.A, Trigger.Go, State.C)] // FSM001
                        [Transition(State.B, Trigger.Back, State.A, Guard = nameof(BadGuard))] // FSM003
                        // State.C is unreachable from State.B // FSM002
                        private void Config() { }
                        
                        private string BadGuard() => ""wrong""; // Wrong return type
                    }
                }";

            
        var (asm1, diags1, generatedSources1) =
            CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        // Assert
        Assert.Contains(diags1, d => d.Id ==RuleIdentifiers.DuplicateTransition);
        Assert.Contains(diags1, d => d.Id == RuleIdentifiers.UnreachableState); 
        Assert.Contains(diags1, d => d.Id == RuleIdentifiers.InvalidMethodSignature);

        output.WriteLine($"Total diagnostics found: {diags1.Length}");
        foreach (var diag in diags1.Where(d => d.Id.StartsWith("FSM")))
        {
            output.WriteLine($"  {diag.Id}: {diag.GetMessage()}");
        }
    }

    [Fact]
    public void ComplexGuardSignatures_ReportCorrectly()
    {
        // Test various invalid guard signatures
        const string sourceCode = @"
                using Abstractions.Attributes;
                namespace Test {
                    public enum State { A, B }
                    public enum Trigger { T1, T2, T3, T4 }
                    
                    [StateMachine(typeof(State), typeof(Trigger))]
                    public partial class Machine {
                        [Transition(State.A, Trigger.T1, State.B, Guard = nameof(GuardWithParams))]
                        [Transition(State.A, Trigger.T2, State.B, Guard = nameof(GuardReturnsVoid))]
                        [Transition(State.A, Trigger.T3, State.B, Guard = nameof(GuardReturnsInt))]
                        [Transition(State.A, Trigger.T4, State.B, Guard = nameof(ValidGuard))] // This one is OK
                        private void Config() { }
                        
                        private bool GuardWithParams(int x) => true; // Wrong - has parameters
                        private void GuardReturnsVoid() { } // Wrong - returns void
                        private int GuardReturnsInt() => 1; // Wrong - returns int
                        private bool ValidGuard() => true; // Correct
                    }
                }";

        var (asm1, diags1, generatedSources1) =
            CompileAndRunGenerator([sourceCode], new StateMachineGenerator());

        // Should have 3 FSM003 errors
        var fsmErrors = diags1.Where(d => d.Id ==RuleIdentifiers.InvalidMethodSignature).ToList();
        Assert.Equal(3, fsmErrors.Count);

        // Check specific error messages
        Assert.Contains(fsmErrors, d => d.GetMessage().Contains("GuardWithParams"));
        Assert.Contains(fsmErrors, d => d.GetMessage().Contains("GuardReturnsVoid"));
        Assert.Contains(fsmErrors, d => d.GetMessage().Contains("GuardReturnsInt"));
    }

    [Fact]
    public void StateCallbackSignatures_ValidatedCorrectly()
    {
        const string sourceCode = @"
                using Abstractions.Attributes;
                namespace Test {
                    public enum State { A, B }
                    public enum Trigger { Go }
                    
                    [StateMachine(typeof(State), typeof(Trigger))]
                    public partial class Machine {
                        [State(State.A, OnEntry = nameof(BadOnEntry), OnExit = nameof(BadOnExit))]
                        [State(State.B, OnEntry = nameof(GoodOnEntry))]
                        private void ConfigureStates() { }
                        
                        [Transition(State.A, Trigger.Go, State.B)]
                        private void Config() { }
                        
                        private bool BadOnEntry() => true; // Wrong - should return void
                        private void BadOnExit(int x) { } // Wrong - should have no parameters
                        private void GoodOnEntry() { } // Correct
                    }
                }";

        var (asm1, diags1, generatedSources1) =
            CompileAndRunGenerator([sourceCode], new StateMachineGenerator());

        // Should have FSM003 errors for bad callbacks
        var fsmErrors = diags1.Where(d => d.Id == RuleIdentifiers.InvalidMethodSignature).ToList();
        Assert.Equal(2, fsmErrors.Count);
    }

    [Fact]
    public void PartialKeyword_RequiredForGeneration()
    {
        const string sourceCode = @"
                using Abstractions.Attributes;
                namespace Test {
                    public enum State { A }
                    public enum Trigger { Go }
                    
                    [StateMachine(typeof(State), typeof(Trigger))]
                    public class Machine { // Missing 'partial'
                        private void Config() { }
                    }
                }";

        var (asm1, diags1, generatedSources1) =
            CompileAndRunGenerator([sourceCode], new StateMachineGenerator());

        // Should have FSM004 warning
        Assert.Contains(diags1, d => d.Id == RuleIdentifiers.MissingStateMachineAttribute && d.GetMessage().Contains("partial"));
    }

    [Fact]
    public void EnumValueCasting_ValidatedCorrectly()
    {
        const string sourceCode = @"
                using Abstractions.Attributes;
                namespace Test {
                    public enum State : byte { Low = 0, High = 255 }
                    public enum Trigger { Go }
                    
                    [StateMachine(typeof(State), typeof(Trigger))]
                    public partial class Machine {
                        [Transition((State)0, Trigger.Go, (State)255)] // Valid
                        [Transition((State)128, Trigger.Go, State.Low)] // Invalid - 128 not defined
                        private void Config() { }
                    }
                }";

        var (asm1, diags1, generatedSources1) =
            CompileAndRunGenerator([sourceCode], new StateMachineGenerator());

        // Should have one FSM006 for invalid value
        var fsmErrors = diags1.Where(d => d.Id == RuleIdentifiers.InvalidEnumValueInTransition).ToList();
        Assert.Single(fsmErrors);
        Assert.Contains("128", fsmErrors[0].GetMessage());
    }


  
}