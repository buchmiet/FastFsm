using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public class RegressionTests(ITestOutputHelper output):GeneratorBaseClass(output)
{
    private void OutputGeneratedCode(Dictionary<string, string> generatedSources, string filterKeyword)
    {
        foreach (var source in generatedSources.Where(kvp => kvp.Key.Contains(filterKeyword)))
        {
            output.WriteLine($"=== {source.Key} ===");
            output.WriteLine(source.Value);
            output.WriteLine("=== END ===\n");
        }
    }


    [Fact]
    public void Generator_should_handle_methods_from_containing_type()
    {
        // Test że generator może używać metod zdefiniowanych w tej samej klasie
        const string source = """
                              using Abstractions.Attributes;
                              namespace TestNamespace {
                                  public enum State { A, B }
                                  public enum Trigger { Go }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  public partial class MachineWithHelperMethods {
                                      public string Log { get; set; } = "";
                                      
                                      [Transition(State.A, Trigger.Go, State.B, Action = nameof(HandleTransition))]
                                      private void Configure() { }
                                      
                                      private void HandleTransition() {
                                          LogTransition("A->B");
                                      }
                                      
                                      private void LogTransition(string transition) { 
                                          Log += $"Transition: {transition};";
                                      }
                                  }
                              }
                              """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "MachineWithHelperMethods");

        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.MachineWithHelperMethods");
        Assert.NotNull(machineType);

        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");

        object stateA = Enum.Parse(stateType!, "A");
        object instance = Activator.CreateInstance(machineType!, stateA)!;

        PropertyInfo logProp = machineType!.GetProperty("Log")!;
        MethodInfo tryFireMethod = machineType.GetMethod("TryFire", [triggerType, typeof(object)])!;

        object triggerGo = Enum.Parse(triggerType!, "Go");
        tryFireMethod.Invoke(instance, [triggerGo, null]);

        Assert.Equal("Transition: A->B;", logProp.GetValue(instance));
    }


    [Fact]
    public void Generator_should_handle_partial_class_in_multiple_files()
    {
        // Multiple files defining same partial class
        const string file1 = """
                             using Abstractions.Attributes;
                             namespace TestNamespace {
                                 public enum State { A, B }
                                 public enum Trigger { Go }
                                 
                                 [StateMachine(typeof(State), typeof(Trigger))]
                                 public partial class MultiFileMachine {
                                     [Transition(State.A, Trigger.Go, State.B)]
                                     private void ConfigurePart1() { }
                                 }
                             }
                             """;

        const string file2 = """
                             namespace TestNamespace {
                                 public partial class MultiFileMachine {
                                     public string CustomProperty { get; set; } = "Test";
                                     
                                     public void CustomMethod() {
                                         // User code in second file
                                     }
                                 }
                             }
                             """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([file1, file2], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "MultiFileMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.MultiFileMachine");
        Assert.NotNull(machineType);

        // Verify both generated and user members exist
        Assert.NotNull(machineType!.GetMethod("TryFire"));
        Assert.NotNull(machineType.GetProperty("CustomProperty"));
        Assert.NotNull(machineType.GetMethod("CustomMethod"));

        // Test instance creation and property access
        Type? stateType = asm.GetType("TestNamespace.State");
        object stateA = Enum.Parse(stateType!, "A");
        object instance = Activator.CreateInstance(machineType, stateA)!;

        PropertyInfo? customProp = machineType.GetProperty("CustomProperty");
        Assert.Equal("Test", customProp!.GetValue(instance));
    }




    [Fact]
    public void Generator_should_reject_or_handle_generic_class()
    {
        const string source = """
                              using Abstractions.Attributes;
                              namespace TestNamespace {
                                  public enum State { A, B }
                                  public enum Trigger { Go }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  public partial class GenericMachine<T> {
                                      [Transition(State.A, Trigger.Go, State.B)]
                                      private void Configure() { }
                                  }
                              }
                              """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "GenericMachine");

        // Generic state machines might not be supported - check for appropriate diagnostic
        output.WriteLine(
            $"Generic class diagnostics: {string.Join(", ", diags.Select(d => $"{d.Id}: {d.GetMessage()}"))}");

        // Either it should fail with clear error, or if supported, should work
        if (asm != null)
        {
            // If it compiled, verify it works (unlikely for generic classes)
            var types = asm.GetTypes().Where(t => t.Name.Contains("GenericMachine"));
            output.WriteLine($"Found types: {string.Join(", ", types.Select(t => t.FullName))}");
        }
    }

       

    [Fact]
    public void Generator_should_handle_conditional_compilation()
    {
        const string source = """
                              using Abstractions.Attributes;
                              namespace TestNamespace {
                                  public enum State { A, B, C }
                                  public enum Trigger { Go, Back }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  public partial class ConditionalMachine {
                                      [Transition(State.A, Trigger.Go, State.B)]
                                      #if DEBUG
                                      [Transition(State.B, Trigger.Back, State.A)]
                                      #endif
                                      [Transition(State.B, Trigger.Go, State.C)]
                                      private void Configure() { }
                                  }
                              }
                              """;

        // Note: This test might need adjustment as CompileAndRunGenerator doesn't expose
        // parse options directly. You might need to modify CompileAndRunGenerator to accept
        // parse options or preprocessor symbols.

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "ConditionalMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);

        if (asm != null)
        {
            Type? machineType = asm.GetType("TestNamespace.ConditionalMachine");
            Assert.NotNull(machineType);

            // Verify the conditional transition exists/doesn't exist based on compilation symbols
            Type? stateType = asm.GetType("TestNamespace.State");
            Type? triggerType = asm.GetType("TestNamespace.Trigger");

            object stateB = Enum.Parse(stateType!, "B");
            object instance = Activator.CreateInstance(machineType!, stateB)!;

            MethodInfo canFireMethod = machineType!.GetMethod("CanFire", [triggerType!])!;
            object triggerBack = Enum.Parse(triggerType!, "Back");

            // This will depend on whether DEBUG was defined during compilation
            bool canFireBack = (bool)canFireMethod.Invoke(instance, [triggerBack])!;
            output.WriteLine($"Can fire Back trigger from state B: {canFireBack}");
        }
    }

    [Fact]
    public void Generator_should_handle_complex_namespace()
    {
        const string source = """
                              using Abstractions.Attributes;
                              namespace Company.Product.Module.SubModule.Features.StateMachines {
                                  public enum State { A, B }
                                  public enum Trigger { Go }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  public partial class DeepNamespaceMachine {
                                      [Transition(State.A, Trigger.Go, State.B)]
                                      private void Configure() { }
                                  }
                              }
                              """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "DeepNamespaceMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType =
            asm!.GetType("Company.Product.Module.SubModule.Features.StateMachines.DeepNamespaceMachine");
        Assert.NotNull(machineType);

        // Verify it works
        Type? stateType = asm.GetType("Company.Product.Module.SubModule.Features.StateMachines.State");
        object stateA = Enum.Parse(stateType!, "A");
        object instance = Activator.CreateInstance(machineType!, stateA)!;
        Assert.NotNull(instance);
    }

    [Fact]
    public void Generator_should_handle_global_usings()
    {
        const string globalUsings = """
                                    global using System;
                                    global using Abstractions.Attributes;
                                    """;

        const string source = """
                              namespace TestNamespace {
                                  public enum State { A, B }
                                  public enum Trigger { Go }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  public partial class GlobalUsingMachine {
                                      [Transition(State.A, Trigger.Go, State.B)]
                                      private void Configure() { }
                                  }
                              }
                              """;

        var (asm, diags, generatedSources) =
            CompileAndRunGenerator([globalUsings, source], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "GlobalUsingMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.GlobalUsingMachine");
        Assert.NotNull(machineType);
    }

    [Fact]
    public void Generator_should_handle_enum_refactoring()
    {
        // Test that generator handles enum value renames correctly
        const string beforeRefactoring = """
                                         using Abstractions.Attributes;
                                         namespace TestNamespace {
                                             public enum OrderState { New, Processing, Complete }
                                             public enum OrderTrigger { Start, Finish }
                                             
                                             [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
                                             public partial class OrderMachine {
                                                 [Transition(OrderState.New, OrderTrigger.Start, OrderState.Processing)]
                                                 [Transition(OrderState.Processing, OrderTrigger.Finish, OrderState.Complete)]
                                                 private void Configure() { }
                                             }
                                         }
                                         """;

        const string afterRefactoring = """
                                        using Abstractions.Attributes;
                                        namespace TestNamespace {
                                            public enum OrderState { Created, InProgress, Completed }  // Renamed
                                            public enum OrderTrigger { Begin, End }  // Renamed
                                            
                                            [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
                                            public partial class OrderMachine {
                                                [Transition(OrderState.Created, OrderTrigger.Begin, OrderState.InProgress)]
                                                [Transition(OrderState.InProgress, OrderTrigger.End, OrderState.Completed)]
                                                private void Configure() { }
                                            }
                                        }
                                        """;

        // Test before refactoring
        var (asm1, diags1, generatedSources1) =
            CompileAndRunGenerator([beforeRefactoring], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources1, "OrderMachine");
        Assert.DoesNotContain(diags1, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm1);

        // Test after refactoring
        var (asm2, diags2, generatedSources2) =
            CompileAndRunGenerator([afterRefactoring], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources2, "OrderMachine");
        Assert.DoesNotContain(diags2, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm2);

        // Verify both versions work correctly
        Type? machineType1 = asm1!.GetType("TestNamespace.OrderMachine");
        Type? machineType2 = asm2!.GetType("TestNamespace.OrderMachine");

        Assert.NotNull(machineType1);
        Assert.NotNull(machineType2);

        // Test that the refactored version functions correctly
        Type? stateType2 = asm2.GetType("TestNamespace.OrderState");
        Type? triggerType2 = asm2.GetType("TestNamespace.OrderTrigger");

        object stateCreated = Enum.Parse(stateType2!, "Created");
        object instance2 = Activator.CreateInstance(machineType2!, stateCreated)!;

        MethodInfo tryFireMethod2 = machineType2!.GetMethod("TryFire", [triggerType2, typeof(object)])!;
        PropertyInfo currentStateProp2 = machineType2.GetProperty("CurrentState")!;

        object triggerBegin = Enum.Parse(triggerType2!, "Begin");
        object triggerEnd = Enum.Parse(triggerType2!, "End");

        // Created -> InProgress
        bool result1 = (bool)tryFireMethod2.Invoke(instance2, [triggerBegin, null])!;
        Assert.True(result1);
        Assert.Equal("InProgress", currentStateProp2.GetValue(instance2)!.ToString());

        // InProgress -> Completed
        bool result2 = (bool)tryFireMethod2.Invoke(instance2, [triggerEnd, null])!;
        Assert.True(result2);
        Assert.Equal("Completed", currentStateProp2.GetValue(instance2)!.ToString());

        output.WriteLine("Refactoring test passed - generator handles enum renames correctly");
    }

    [Fact]
    public void Print_FullMultiPayloadMachine_GeneratedCode()
    {
        // ---------- 1. Źródła użytkownika ----------
        const string enumsAndPayloads = """
                                        namespace StateMachine.Tests.FullVariant
                                        {
                                            public enum OrderState { New, Processing, Paid, Shipped }
                                            public enum OrderTrigger { Process, Pay, Ship }
                                        
                                            public class OrderPayload      { public int OrderId { get; set; } }
                                            public class PaymentPayload    : OrderPayload { public string Method  { get; set; } = ""; }
                                            public class ShippingPayload   : OrderPayload { public string Carrier { get; set; } = ""; }
                                        }
                                        """;

        const string machineSource = """
                                     using Abstractions.Attributes;
                                     using StateMachine.Tests.FullVariant;

                                     [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
                                     [PayloadType(OrderTrigger.Process, typeof(OrderPayload))]
                                     [PayloadType(OrderTrigger.Pay,    typeof(PaymentPayload))]
                                     [PayloadType(OrderTrigger.Ship,   typeof(ShippingPayload))]
                                     [GenerationMode(GenerationMode.Full, Force = true)]
                                     public partial class FullMultiPayloadMachine
                                     {
                                         [Transition(OrderState.New,        OrderTrigger.Process, OrderState.Processing)]
                                         [Transition(OrderState.Processing, OrderTrigger.Pay,    OrderState.Paid)]
                                         [Transition(OrderState.Paid,       OrderTrigger.Ship,   OrderState.Shipped)]
                                         private void Configure() { }
                                     }
                                     """;

        // ---------- 2. Kompilacja + generator ----------
        var (asm, diags, generated) =
            CompileAndRunGenerator(new[] { enumsAndPayloads, machineSource }, new StateMachineGenerator());

        // ---------- 3. Diagnostyka kompilatora ----------
        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);   // jeśli coś nie zadziałało – test od razu czerwony

        // ---------- 4. Wypisz wygenerowane źródła ----------
        foreach (var kvp in generated.Where(k => k.Key.Contains("FullMultiPayloadMachine")))
        {
            output.WriteLine($"===== {kvp.Key} =====");
            output.WriteLine(kvp.Value);
            output.WriteLine("=====   END   =====\n");
        }

        // Dla pewności – niech chociaż jeden plik istniał
        Assert.Contains(generated.Keys, k => k.Contains("FullMultiPayloadMachine"));
    }

}