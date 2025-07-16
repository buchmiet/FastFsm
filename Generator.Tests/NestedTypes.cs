using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

/// <summary>
/// Testy sprawdzające obsługę typów zagnieżdżonych przez generator,
/// zaktualizowane do nowego API testowego.
/// </summary>
public class NestedTypesTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    // Pomocnicza metoda do wyświetlania wygenerowanego kodu w razie potrzeby
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
    public void Generator_should_handle_nested_enums_correctly()
    {
        // Arrange
        const string source = @"
using Abstractions.Attributes;

namespace TestNamespace
{
    public static class Container
    {
        public enum NestedState { Idle, Active, Done }
        public enum NestedTrigger { Start, Stop, Reset }
        
        [StateMachine(typeof(NestedState), typeof(NestedTrigger))]
        public partial class NestedMachine
        {
            [Transition(NestedState.Idle, NestedTrigger.Start, NestedState.Active)]
            private void ConfigureTransitions() { }
        }
    }
}";

        // Act
        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "NestedMachine");

        // Assert
        diags.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        asm.ShouldNotBeNull();

        asm.GetType("TestNamespace.Container+NestedMachine").ShouldNotBeNull();

        // Sprawdzenie poprawnych odwołań do typów w wygenerowanym kodzie
        var generatedFile = generatedSources.Where(kvp => kvp.Key.Contains("NestedMachine.Generated.cs")).ShouldHaveSingleItem().Value;
        generatedFile.ShouldContain("Container.NestedState");
        generatedFile.ShouldContain("Container.NestedTrigger");
    }

    [Fact]
    public void Generator_should_handle_doubly_nested_types_correctly()
    {
        // Arrange
        const string source = @"
using Abstractions.Attributes;

namespace TestNamespace
{
    public class OuterClass
    {
        public class InnerClass
        {
            public enum DeeplyNestedState { One, Two, Three }
            public enum DeeplyNestedTrigger { Next, Previous }
            
            [StateMachine(typeof(DeeplyNestedState), typeof(DeeplyNestedTrigger))]
            public partial class DeeplyNestedMachine
            {
                [Transition(DeeplyNestedState.One, DeeplyNestedTrigger.Next, DeeplyNestedState.Two)]
                [Transition(DeeplyNestedState.Two, DeeplyNestedTrigger.Previous, DeeplyNestedState.One)]
                private void Configure() { }
            }
        }
    }
}";

        // Act
        var (asm, diags, _) = CompileAndRunGenerator([source], new StateMachineGenerator());

        // Assert
        diags.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        asm.ShouldNotBeNull();

        asm.GetType("TestNamespace.OuterClass+InnerClass+DeeplyNestedMachine").ShouldNotBeNull();
    }

    [Fact]
    public void Generator_should_handle_nested_payload_types_correctly()
    {
        // Arrange
        const string source = @"
using Abstractions.Attributes;

namespace TestNamespace
{
    public class Domain
    {
        public class Commands
        {
            public class StartCommand { public int Id { get; set; } }
            public class StopCommand { public string Reason { get; set; } }
        }
        
        public enum State { Idle, Running }
        public enum Trigger { Start, Stop }
        
        [StateMachine(typeof(State), typeof(Trigger))]
        [PayloadType(Trigger.Start, typeof(Commands.StartCommand))]
        [PayloadType(Trigger.Stop, typeof(Commands.StopCommand))]
        public partial class NestedPayloadMachine
        {
            [Transition(State.Idle, Trigger.Start, State.Running)]
            private void OnStart() { }
            
            [Transition(State.Running, Trigger.Stop, State.Idle)]
            private void OnStop() { }
            
            private bool CanStart(Commands.StartCommand cmd) => cmd.Id > 0;
            private void HandleStop(Commands.StopCommand cmd) { }
        }
    }
}";

        // Act
        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "NestedPayloadMachine");

        // Assert
        diags.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        asm.ShouldNotBeNull();

        var generatedFile = generatedSources.Where(kvp => kvp.Key.Contains("NestedPayloadMachine.Generated.cs")).ShouldHaveSingleItem().Value;
        generatedFile.ShouldContain("global::TestNamespace.Domain.Commands.StartCommand");
        generatedFile.ShouldContain("global::TestNamespace.Domain.Commands.StopCommand");
    }

    [Fact]
    public void Generator_should_handle_generic_nested_types_as_payload()
    {
        // Arrange
        const string source = @"
using Abstractions.Attributes;
using System.Collections.Generic;

namespace TestNamespace
{
    public class Events
    {
        public class Event<T> { public T Data { get; set; } }
    }
    
    public enum State { Init, Processing }
    public enum Trigger { Process, Reset }
    
    [StateMachine(typeof(State), typeof(Trigger))]
    [PayloadType(Trigger.Process, typeof(Events.Event<string>))]
    public partial class GenericNestedMachine
    {
        [Transition(State.Init, Trigger.Process, State.Processing)]
        private void StartProcessing() { }
        
        private void HandleProcess(Events.Event<string> evt) { }
    }
}";

        // Act
        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "GenericNestedMachine");

        // Assert
        diags.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        asm.ShouldNotBeNull();

        var generatedFile = generatedSources.Where(kvp => kvp.Key.Contains("GenericNestedMachine.Generated.cs")).ShouldHaveSingleItem().Value;
        generatedFile.ShouldContain("global::TestNamespace.Events.Event<string>");
    }

    [Fact]
    public void Generator_should_reject_private_nested_types()
    {
        // Arrange
        const string source = @"
using Abstractions.Attributes;

namespace TestNamespace
{
    public class Container
    {
        private enum PrivateState { One, Two }
        public enum PublicTrigger { Go }
        
        [StateMachine(typeof(PrivateState), typeof(PublicTrigger))]
        public partial class InvalidMachine
        {
            [Transition(PrivateState.One, PublicTrigger.Go, PrivateState.Two)]
            private void Configure() { }
        }
    }
}";

        // Act
        var (asm, diags, _) = CompileAndRunGenerator([source], new StateMachineGenerator());

        // Assert
        // Kompilacja powinna się nie udać, więc assembly będzie nullem
        asm.ShouldBeNull();

        // Powinien wystąpić błąd kompilacji dotyczący niedostępności typu
        diags.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id.Contains("CS0122")); // 'PrivateState' is inaccessible due to its protection level
    }
}