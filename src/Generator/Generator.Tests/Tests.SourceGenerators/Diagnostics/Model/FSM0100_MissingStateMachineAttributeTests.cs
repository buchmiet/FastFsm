using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators;

public class FSM0100_MissingStateMachineAttributeTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM0100_When_Class_Uses_Transition_But_Lacks_StateMachine_Attribute()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { Go }

    // Missing [StateMachine(typeof(State), typeof(Trigger))]
    public class Machine {
        [Transition(State.A, Trigger.Go, State.B)]
        private void Config() { }
    }
}
";

        var tree = CSharpSyntaxTree.ParseText(src, CSharpParseOptions.Default);
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();
        // Ensure project refs (Abstractions/FastFsm) are present for attribute resolution
        AddProjectReferences(refs);

        var compilation = CSharpCompilation.Create(
            "FsmTestAnalyzerOnly",
            new[] { tree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new Generator.Analyzers.StateMachineAnalyzer();
        var diags = compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync().Result;

        var hits = diags.Where(d => d.Id == RuleIdentifiers.MissingStateMachineAttribute).ToList();
        Assert.True(hits.Count >= 1, "Expected at least one FSM0100 diagnostic.");
    }
}
