using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

/// <summary>
/// Diagnostic test to understand why the test environment isn't working
/// </summary>
public class EnvironmentDiagnosticTest(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void TestEnvironment_BasicGenerator()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class TestMachine { }
            }";

        output.WriteLine("=== ENVIRONMENT DIAGNOSTIC TEST ===\n");
        
        var (asm, diags, generatedSources) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        // 1. Check if any code was generated
        output.WriteLine($"1. Generated sources count: {generatedSources.Count}");
        foreach (var src in generatedSources)
        {
            output.WriteLine($"   - {src.Key}: {src.Value.Length} chars");
        }
        
        // 2. Check all diagnostics
        output.WriteLine($"\n2. Total diagnostics: {diags.Length}");
        var grouped = diags.GroupBy(d => d.Id).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            output.WriteLine($"   - {group.Key}: {group.Count()} occurrences");
            foreach (var diag in group.Take(2))
            {
                output.WriteLine($"     {diag.GetMessage()}");
            }
        }
        
        // 3. Check for FSM-specific diagnostics
        var fsmDiags = diags.Where(d => d.Id.StartsWith("FSM")).ToList();
        output.WriteLine($"\n3. FSM diagnostics: {fsmDiags.Count}");
        foreach (var diag in fsmDiags)
        {
            output.WriteLine($"   - {diag.Id}: {diag.GetMessage()}");
        }
        
        // 4. Check compilation success
        output.WriteLine($"\n4. Assembly created: {asm != null}");
        
        // 5. Check for specific error patterns
        var cs0246 = diags.Where(d => d.Id == "CS0246").ToList(); // Type or namespace not found
        output.WriteLine($"\n5. CS0246 errors (type not found): {cs0246.Count}");
        foreach (var err in cs0246)
        {
            output.WriteLine($"   - {err.GetMessage()}");
        }
    }
    
    [Fact]
    public void TestEnvironment_AttributeVisibility()
    {
        const string sourceCode = @"
            namespace Test {
                [Abstractions.Attributes.StateMachine(typeof(int), typeof(int))]
                public partial class TestMachine { }
            }";

        output.WriteLine("=== ATTRIBUTE VISIBILITY TEST ===\n");
        
        var (asm, diags, generatedSources) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        // Check if StateMachine attribute caused any errors
        var attributeErrors = diags.Where(d => d.GetMessage().Contains("StateMachine")).ToList();
        output.WriteLine($"Attribute-related errors: {attributeErrors.Count}");
        foreach (var err in attributeErrors)
        {
            output.WriteLine($"   {err.Id}: {err.GetMessage()}");
        }
        
        // Check if generator processed the class
        output.WriteLine($"\nGenerated sources: {generatedSources.Count}");
        
        // Look for FSM998 (candidate found) diagnostic
        var candidateFound = diags.Any(d => d.Id == "FSM998");
        output.WriteLine($"FSM998 (candidate found): {candidateFound}");
    }
    
    [Fact]
    public void TestEnvironment_SimpleTransition()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum S { A, B }
                public enum T { X }
                
                [StateMachine(typeof(S), typeof(T))]
                public partial class Machine {
                    [Transition(S.A, T.X, S.B)]
                    private void Config() { }
                }
            }";

        output.WriteLine("=== SIMPLE TRANSITION TEST ===\n");
        
        var (asm, diags, generatedSources) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        // Count different diagnostic categories
        var errorCount = diags.Count(d => d.Severity == DiagnosticSeverity.Error);
        var warningCount = diags.Count(d => d.Severity == DiagnosticSeverity.Warning);
        var infoCount = diags.Count(d => d.Severity == DiagnosticSeverity.Info);
        
        output.WriteLine($"Errors: {errorCount}, Warnings: {warningCount}, Info: {infoCount}");
        
        // Check for critical diagnostics
        var fsmDiags = diags.Where(d => d.Id.StartsWith("FSM")).ToList();
        output.WriteLine($"\nFSM Diagnostics ({fsmDiags.Count}):");
        foreach (var diag in fsmDiags.OrderBy(d => d.Id))
        {
            output.WriteLine($"  {diag.Id} [{diag.Severity}]: {diag.GetMessage()}");
        }
        
        // Check if any source was generated
        var hasGeneratedCode = generatedSources.Any(s => s.Value.Contains("class Machine"));
        output.WriteLine($"\nGenerated Machine class: {hasGeneratedCode}");
        
        if (generatedSources.Any())
        {
            var firstSource = generatedSources.First();
            output.WriteLine($"First 500 chars of generated code:");
            output.WriteLine(firstSource.Value.Substring(0, Math.Min(500, firstSource.Value.Length)));
        }
    }
    
    [Fact]
    public void TestEnvironment_CheckReferences()
    {
        output.WriteLine("=== REFERENCE CHECK ===\n");
        
        // Create a minimal compilation to check references
        var tree = CSharpSyntaxTree.ParseText(@"
            using Abstractions.Attributes;
            class Test { }");
        
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();
        
        // Add project references
        AddProjectReferences(refs);
        
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        // Check if Abstractions types can be resolved
        var abstractionsNamespace = compilation.GetTypeByMetadataName("Abstractions.Attributes.StateMachineAttribute");
        output.WriteLine($"StateMachineAttribute resolved: {abstractionsNamespace != null}");
        
        var transitionAttr = compilation.GetTypeByMetadataName("Abstractions.Attributes.TransitionAttribute");
        output.WriteLine($"TransitionAttribute resolved: {transitionAttr != null}");
        
        // List all referenced assemblies
        output.WriteLine($"\nTotal references: {refs.Count}");
        var abstractionsRef = refs.FirstOrDefault(r => r.Display?.Contains("Abstractions") == true);
        if (abstractionsRef != null)
        {
            output.WriteLine($"Abstractions reference: {abstractionsRef.Display}");
        }
        else
        {
            output.WriteLine("WARNING: No Abstractions reference found!");
        }
        
        // Check compilation diagnostics
        var compileDiags = compilation.GetDiagnostics();
        var errors = compileDiags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        output.WriteLine($"\nCompilation errors: {errors.Count}");
        foreach (var err in errors.Take(5))
        {
            output.WriteLine($"  {err.Id}: {err.GetMessage()}");
        }
    }
}