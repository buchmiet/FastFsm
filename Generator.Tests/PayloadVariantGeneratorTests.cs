using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public class PayloadVariantGeneratorTests(ITestOutputHelper output) : GeneratorBaseClass(output)
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

    // 1. Test for default payload type
    [Fact]
    public void Generator_should_compile_machine_with_default_payload_type()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Idle, Processing, Done }
            public enum Trigger { Start, Process, Complete }
            
            public class ProcessingContext 
            { 
                public string Data { get; set; } = "";
                public int Progress { get; set; }
            }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(ProcessingContext))] // Default payload for all triggers
            public partial class PayloadMachine
            {
                public string LastPayloadData { get; private set; } = "";

                [Transition(State.Idle, Trigger.Start, State.Processing)]
                [Transition(State.Processing, Trigger.Process, State.Processing)]
                [Transition(State.Processing, Trigger.Complete, State.Done)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "PayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.PayloadMachine");
        Assert.NotNull(machineType);

        // Check interface - should implement IStateMachineWithPayload
        Type? interfaceType = asm.GetType("TestNamespace.IPayloadMachine");
        Assert.NotNull(interfaceType);

        // Verify TryFire with payload exists
        Type? payloadType = asm.GetType("TestNamespace.ProcessingContext");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");
        Assert.NotNull(machineType!.GetMethod("TryFire", [triggerType!, payloadType!]));
        Assert.NotNull(machineType.GetMethod("Fire", [triggerType!, payloadType!]));
    }

    // 2. Test for transition-specific payload types
    [Fact]
    public void Generator_should_compile_machine_with_transition_specific_payload_types()
    {
        const string userSource = """
        using Abstractions.Attributes;
        using System; 
        namespace TestNamespace {
            public enum State { Ready, Processing, Completed }
            public enum Trigger { StartWithConfig, ProcessData, CompleteWithResult }
            
            public class StartConfig { public string Mode { get; set; } = ""; }
            public class ProcessData { public byte[] Data { get; set; } = Array.Empty<byte>(); }
            public class CompletionResult { public bool Success { get; set; } }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class MultiPayloadMachine
            {
                [PayloadType(Trigger.StartWithConfig, typeof(StartConfig))]
                [PayloadType(Trigger.ProcessData, typeof(ProcessData))]
                [PayloadType(Trigger.CompleteWithResult, typeof(CompletionResult))]
                private void ConfigurePayloads() { }

                [Transition(State.Ready, Trigger.StartWithConfig, State.Processing)]
                [Transition(State.Processing, Trigger.ProcessData, State.Processing)]
                [Transition(State.Processing, Trigger.CompleteWithResult, State.Completed)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "MultiPayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.MultiPayloadMachine");
        Assert.NotNull(machineType);

        // Should implement IStateMachineWithMultiPayload
        Type? interfaceType = asm.GetType("TestNamespace.IMultiPayloadMachine");
        Assert.NotNull(interfaceType);

        // Check for generic TryFire method
        var tryFireMethods = machineType!.GetMethods()
            .Where(m => m.Name == "TryFire" && m.IsGenericMethodDefinition)
            .ToList();
        Assert.NotEmpty(tryFireMethods);
    }

    // 3. Test for mixed payload and no-payload transitions
    [Fact]
    public void Generator_should_compile_machine_with_mixed_payload_and_no_payload_transitions()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Idle, Working, Paused, Done }
            public enum Trigger { Start, Pause, Resume, Complete }
            
            public class WorkContext { public string WorkItem { get; set; } = ""; }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class MixedPayloadMachine
            {
                [PayloadType(Trigger.Start, typeof(WorkContext))]
                private void ConfigurePayloads() { }

                [Transition(State.Idle, Trigger.Start, State.Working)] // Has payload
                [Transition(State.Working, Trigger.Pause, State.Paused)] // No payload
                [Transition(State.Paused, Trigger.Resume, State.Working)] // No payload
                [Transition(State.Working, Trigger.Complete, State.Done)] // No payload
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "MixedPayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.MixedPayloadMachine");
        Assert.NotNull(machineType);

        // Should still generate multi-payload variant
        Assert.NotNull(asm.GetType("TestNamespace.IMixedPayloadMachine"));
    }

    // 4. Test for payload in internal transitions
    [Fact]
    public void Generator_should_handle_payload_in_internal_transitions()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Active }
            public enum Trigger { Update, Refresh }
            
            public class UpdateData { public int Value { get; set; } }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(UpdateData))]
            public partial class InternalPayloadMachine
            {
                public int LastValue { get; private set; }

                [InternalTransition(State.Active, Trigger.Update, nameof(HandleUpdate))]
                [InternalTransition(State.Active, Trigger.Refresh, nameof(HandleRefresh))]
                private void ConfigureTr() { }

                private void HandleUpdate(UpdateData data) => LastValue = data.Value;
                private void HandleRefresh() => LastValue = 0;
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "InternalPayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.InternalPayloadMachine");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");
        Type? payloadType = asm.GetType("TestNamespace.UpdateData");

        object stateActive = Enum.Parse(stateType!, "Active");
        object machine = Activator.CreateInstance(machineType!, stateActive)!;

        MethodInfo tryFireMethod = machineType!.GetMethod("TryFire", [triggerType!, payloadType!])!;
        PropertyInfo lastValueProp = machineType.GetProperty("LastValue")!;

        object triggerUpdate = Enum.Parse(triggerType!, "Update");
        object updateData = Activator.CreateInstance(payloadType!)!;
        payloadType!.GetProperty("Value")!.SetValue(updateData, 42);

        bool result = (bool)tryFireMethod.Invoke(machine, [triggerUpdate, updateData])!;
        Assert.True(result);
        Assert.Equal(42, lastValueProp.GetValue(machine));
    }

    // 5. Test for nested payload types
    [Fact]
    public void Generator_should_handle_nested_payload_types()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Ready, Processing }
            public enum Trigger { Process }
            
            public class OuterClass
            {
                public class NestedPayload
                {
                    public string NestedData { get; set; } = "";
                }
            }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(OuterClass.NestedPayload))]
            public partial class NestedPayloadMachine
            {
                [Transition(State.Ready, Trigger.Process, State.Processing)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "NestedPayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.NestedPayloadMachine");
        Assert.NotNull(machineType);

        // Check that nested type is properly referenced
        Type? nestedPayloadType = asm.GetType("TestNamespace.OuterClass+NestedPayload");
        Assert.NotNull(nestedPayloadType);
    }

    // 6. Test for factory generation with payload
    [Fact]
    public void Generator_should_generate_factory_for_payload_machine()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Init, Running }
            public enum Trigger { Start }
            
            public class Config { public string Setting { get; set; } = ""; }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(Config))]
            public partial class FactoryPayloadMachine
            {
                [Transition(State.Init, Trigger.Start, State.Running)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator(), enableLogging: true, enableDependencyInjection: true);

        // Check that factory file was generated
        Assert.Contains(generatedSources.Keys, k => k.Contains("FactoryPayloadMachine.Factory.g.cs"));

        OutputGeneratedCode(generatedSources, "FactoryPayloadMachine.Factory");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        // Verify factory class exists
        Type? factoryType = asm!.GetType("TestNamespace.FactoryPayloadMachineFactory");
        Assert.NotNull(factoryType);
    }

    // 7. Test for proper interface generation
    [Fact]
    public void Generator_should_generate_proper_interface_for_payload_machine()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B }
            public enum Trigger { Go }
            
            public class MyPayload { public int Id { get; set; } }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(MyPayload))]
            public partial class InterfacePayloadMachine
            {
                [Transition(State.A, Trigger.Go, State.B)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "InterfacePayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? interfaceType = asm!.GetType("TestNamespace.IInterfacePayloadMachine");
        Assert.NotNull(interfaceType);

        // Should implement IStateMachineWithPayload<TState, TTrigger, TPayload>
        var implementedInterfaces = interfaceType!.GetInterfaces();
        Assert.Contains(implementedInterfaces, i => i.Name.StartsWith("IStateMachineWithPayload"));
    }

    [Fact]
    public void Generator_should_not_generate_code_when_forcing_pure_with_payload()
    {
        const string userSource = """
                                  using Abstractions.Attributes;
                                  namespace TestNamespace {
                                      public enum State { A, B }
                                      public enum Trigger { Go }
                                      
                                      public class MyData { }
                                      [StateMachine(typeof(State), typeof(Trigger))]
                                      [GenerationMode(GenerationMode.Pure, Force = true)]
                                      [PayloadType(typeof(MyData))] // This should cause FSM009 error
                                      public partial class ForcedPureWithPayload
                                      {
                                          [Transition(State.A, Trigger.Go, State.B)]
                                          private void ConfigureTr() { }
                                      }
                                  }
                                  """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());

        // Should report FSM009 error
        var fsm009 = diags.FirstOrDefault(d => d.Id == "FSM009");
        Assert.NotNull(fsm009);
        Assert.Equal(DiagnosticSeverity.Error, fsm009.Severity);

        // Should NOT generate any code for this machine
        Assert.DoesNotContain(generatedSources.Keys, k => k.Contains("ForcedPureWithPayload"));

        // But the assembly might still compile (with just the partial class stub)
        // The important thing is that no state machine code was generated
    }

    // 9. Test for generic payload types
    [Fact]
    public void Generator_should_handle_payload_with_generic_types()
    {
        const string userSource = """
        using Abstractions.Attributes;
        using System.Collections.Generic;
        namespace TestNamespace {
            public enum State { Empty, HasData }
            public enum Trigger { AddData, Clear }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class GenericPayloadMachine
            {
                [PayloadType(Trigger.AddData, typeof(List<string>))]
                private void ConfigurePayloads() { }

                [Transition(State.Empty, Trigger.AddData, State.HasData)]
                [Transition(State.HasData, Trigger.Clear, State.Empty)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "GenericPayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.GenericPayloadMachine");
        Assert.NotNull(machineType);
    }

    // 10. Test for payload inheritance
    [Fact]
    public void Generator_should_handle_payload_inheritance()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Waiting, Processing }
            public enum Trigger { Process }
            
            public abstract class BaseCommand { public int Id { get; set; } }
            public class SpecificCommand : BaseCommand { public string Details { get; set; } = ""; }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(BaseCommand))] // Base type as payload
            public partial class InheritancePayloadMachine
            {
                public BaseCommand? LastCommand { get; private set; }

                [Transition(State.Waiting, Trigger.Process, State.Processing, Action = nameof(StoreCommand))]
                private void ConfigureTr() { }

                private void StoreCommand(BaseCommand cmd) => LastCommand = cmd;
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "InheritancePayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.InheritancePayloadMachine");
        Assert.NotNull(machineType);
    }

    // 11. Test for actions with payload parameters
    [Fact]
    public void Generator_should_pass_payload_to_actions_with_correct_signature()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Start, End }
            public enum Trigger { Execute }
            
            public class ActionContext { public string Message { get; set; } = ""; }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(ActionContext))]
            public partial class ActionPayloadMachine
            {
                public string ReceivedMessage { get; private set; } = "";

                [Transition(State.Start, Trigger.Execute, State.End, Action = nameof(ProcessAction))]
                private void ConfigureTr() { }

                private void ProcessAction(ActionContext ctx) => ReceivedMessage = ctx.Message;
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "ActionPayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.ActionPayloadMachine");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");
        Type? payloadType = asm.GetType("TestNamespace.ActionContext");

        object startState = Enum.Parse(stateType!, "Start");
        object machine = Activator.CreateInstance(machineType!, startState)!;

        MethodInfo tryFireMethod = machineType!.GetMethod("TryFire", [triggerType!, payloadType!])!;
        PropertyInfo messageProp = machineType.GetProperty("ReceivedMessage")!;

        object trigger = Enum.Parse(triggerType!, "Execute");
        object payload = Activator.CreateInstance(payloadType!)!;
        payloadType!.GetProperty("Message")!.SetValue(payload, "Hello from payload");

        bool result = (bool)tryFireMethod.Invoke(machine, [trigger, payload])!;
        Assert.True(result);
        Assert.Equal("Hello from payload", messageProp.GetValue(machine));
    }

    // 12. Test for guards with payload parameters
    [Fact]
    public void Generator_should_pass_payload_to_guards_with_correct_signature()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Locked, Unlocked }
            public enum Trigger { TryUnlock }
            
            public class UnlockRequest { public string Code { get; set; } = ""; }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(UnlockRequest))]
            public partial class GuardPayloadMachine
            {
                private const string CORRECT_CODE = "1234";

                [Transition(State.Locked, Trigger.TryUnlock, State.Unlocked, Guard = nameof(IsValidCode))]
                private void ConfigureTr() { }

                private bool IsValidCode(UnlockRequest request) => request.Code == CORRECT_CODE;
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "GuardPayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.GuardPayloadMachine");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");
        Type? payloadType = asm.GetType("TestNamespace.UnlockRequest");

        object lockedState = Enum.Parse(stateType!, "Locked");
        object machine = Activator.CreateInstance(machineType!, lockedState)!;

        MethodInfo tryFireMethod = machineType!.GetMethod("TryFire", [triggerType!, payloadType!])!;
        PropertyInfo currentStateProp = machineType.GetProperty("CurrentState")!;

        object trigger = Enum.Parse(triggerType!, "TryUnlock");

        // Try with wrong code
        object wrongPayload = Activator.CreateInstance(payloadType!)!;
        payloadType!.GetProperty("Code")!.SetValue(wrongPayload, "wrong");
        bool result1 = (bool)tryFireMethod.Invoke(machine, [trigger, wrongPayload])!;
        Assert.False(result1);
        Assert.Equal("Locked", currentStateProp.GetValue(machine)!.ToString());

        // Try with correct code
        object correctPayload = Activator.CreateInstance(payloadType!)!;
        payloadType!.GetProperty("Code")!.SetValue(correctPayload, "1234");
        bool result2 = (bool)tryFireMethod.Invoke(machine, [trigger, correctPayload])!;
        Assert.True(result2);
        Assert.Equal("Unlocked", currentStateProp.GetValue(machine)!.ToString());
    }

    // 13. Test for payload with state callbacks
    [Fact]
    public void Generator_should_handle_payload_with_state_callbacks()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Init, Active, Done }
            public enum Trigger { Activate, Deactivate }
            
            public class StateContext { public string Reason { get; set; } = ""; }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(StateContext))]
            public partial class StateCallbackPayloadMachine
            {
                public string Log { get; set; } = "";

                [Transition(State.Init, Trigger.Activate, State.Active)]
                [Transition(State.Active, Trigger.Deactivate, State.Done)]
                private void ConfigureTr() { }

                [State(State.Active, OnEntry = nameof(OnActiveEntry), OnExit = nameof(OnActiveExit))]
                private void StateConfig() { }

                private void OnActiveEntry(StateContext ctx) => Log += $"Enter:{ctx.Reason};";
                private void OnActiveExit(StateContext ctx) => Log += $"Exit:{ctx.Reason};";
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "StateCallbackPayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.StateCallbackPayloadMachine");
        Assert.NotNull(machineType);
    }

    // 14. Test for extension methods in DI
    [Fact]
    public void Generator_should_create_extension_methods_for_payload_machine()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Ready }
            public enum Trigger { Go }
            
            public class ServiceContext { public string ServiceId { get; set; } = ""; }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(ServiceContext))]
            public partial class DIPayloadMachine
            {
                [Transition(State.Ready, Trigger.Go, State.Ready)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator(),
            enableLogging: true,            // DI‑pakiet ustawia to domyślnie,
            enableDependencyInjection: true // ← kluczowe dla klas Factory
        );

        // Check factory file contains extension methods
        var factorySource = generatedSources.FirstOrDefault(kvp => kvp.Key.Contains("DIPayloadMachine.Factory")).Value;
        Assert.NotNull(factorySource);

        OutputGeneratedCode(generatedSources, "DIPayloadMachine.Factory");

        // Should contain AddDIPayloadMachine extension method
        Assert.Contains("AddDIPayloadMachine", factorySource);
        Assert.Contains("Microsoft.Extensions.DependencyInjection", factorySource);

        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
    }

    // 15. Test for correct factory interface implementation
    [Fact]
    public void Generator_should_implement_correct_factory_interface_for_payload()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { On, Off }
            public enum Trigger { Toggle }
            
            public class ToggleContext { public bool Force { get; set; } }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(ToggleContext))]
            public partial class FactoryInterfacePayloadMachine
            {
                [Transition(State.On, Trigger.Toggle, State.Off)]
                [Transition(State.Off, Trigger.Toggle, State.On)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator(),
            enableLogging: true,            // DI‑pakiet ustawia to domyślnie,
            enableDependencyInjection: true // ← kluczowe dla klas Factory
        );
        OutputGeneratedCode(generatedSources, "FactoryInterfacePayloadMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? factoryType = asm!.GetType("TestNamespace.FactoryInterfacePayloadMachineFactory");
        Assert.NotNull(factoryType);

        // Check that factory implements IStateMachineWithPayloadFactory
        var interfaces = factoryType!.GetInterfaces();
        Assert.Contains(interfaces, i => i.Name.Contains("IStateMachineWithPayloadFactory"));

        // Should have Create method with payload
        var createMethods = factoryType.GetMethods().Where(m => m.Name == "Create").ToList();
        Assert.Contains(createMethods, m => m.GetParameters().Length == 2); // Create(initialState, defaultPayload)
    }
}